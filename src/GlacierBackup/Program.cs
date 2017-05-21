using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Glacier.Transfer;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using GlacierBackup.FileSearchers;
using GlacierBackup.Writers;


namespace GlacierBackup
{
    public class Program
    {
        const int RETRY_COUNT = 3;
        const int START_WAIT_TIME_MS = 5000;

        static readonly object _lockObj = new object();
        static readonly Random _rand = new Random();

        readonly Options _opts;
        readonly IResultWriter _resultWriter;
        readonly IFileSearcher _searcher;
        readonly int _vpus;
        readonly ArchiveTransferManager _atm;


        internal Program(Options opts)
        {
            _opts = opts;
            _searcher = GetFileSearcher();
            _resultWriter = GetResultWriter();
            _vpus = GetVpus();
            _atm = new ArchiveTransferManager(_opts.Credentials, _opts.Region);
        }


        public static void Main(string[] args)
        {
            if(args.Length != 8)
            {
                ShowUsage();
                Environment.Exit(1);
            }

            var profile = args[0];
            var awsRegion = RegionEndpoint.GetBySystemName(args[1]);
            var vaultName = args[2];
            var backupType = args[3];
            var backupSource = args[4];  // individual file / or folder
            var relativeRoot = args[5];
            var outputType = args[6];
            var output = args[7];  // sql file to write

            AWSCredentials credentials = null;
            var sharedFile = new SharedCredentialsFile();
            
            try
            {
                if (sharedFile.TryGetProfile(profile, out CredentialProfile theProfile))
                {
                    credentials = AWSCredentialsFactory.GetAWSCredentials(theProfile, sharedFile);
                }
                else
                {
                    throw new ApplicationException("Unable to access profile.");
                }
            }
            catch (System.Exception)
            {
                Console.WriteLine($"Unable to obtain credentials for profile [{profile}].  Please make sure this is properly configured in ~/.aws/credentials.");
                Environment.Exit(2);
            }

            if(string.Equals(awsRegion.DisplayName, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"The specified region [{awsRegion.SystemName}] was unknown!  Please select a valid region.");
                Environment.Exit(2);
            }

            BackupType theBackupType;

            if(!Enum.TryParse<BackupType>(backupType, true, out theBackupType))
            {
                Console.WriteLine($"Please specify a valid backup type [Full, Assets, File].");
                Environment.Exit(2);
            }
            
            if(theBackupType == BackupType.File && !File.Exists(backupSource))
            {
                Console.WriteLine($"The specified backup file [{backupSource}] does not exist.  Please enter a valid directory path to backup.");
                Environment.Exit(2);
            }
            else if((theBackupType == BackupType.Assets || theBackupType == BackupType.Full) && !Directory.Exists(backupSource))
            {
                Console.WriteLine($"The specified backup directory [{backupSource}] does not exist.  Please enter a valid directory path to backup.");
                Environment.Exit(2);
            }
            else if(theBackupType == BackupType.List && !File.Exists(backupSource))
            {
                Console.WriteLine($"The specified file containing the list of files to backup [{backupSource}] does not exist.  Please enter a valid path to the list file.");
                Environment.Exit(2);
            }

            if(theBackupType != BackupType.List && relativeRoot.Length > backupSource.Length)
            {
                Console.WriteLine("The relative_root path should be the starting part of the path to the backup to remove, such that the remaining path is tracked as the archive description.");
                Environment.Exit(2);
            }

            if(theBackupType != BackupType.List && !backupSource.StartsWith(relativeRoot))
            {
                Console.WriteLine("The relative_root should exactly match the same starting path to the backup_source.");
                Environment.Exit(2);
            }

            OutputType theOutputType;

            if(!Enum.TryParse<OutputType>(outputType, true, out theOutputType))
            {
                Console.WriteLine($"Please specify a valid output type [PhotoSql, VideoSql, Csv].");
                Environment.Exit(2);
            }

            if(File.Exists(output))
            {
                Console.WriteLine("Output file already exists - exiting!");
                Environment.Exit(2);
            }

            var program = new Program(new Options {
                Credentials = credentials, 
                Region = awsRegion, 
                VaultName = vaultName, 
                BackupType = theBackupType, 
                BackupSource = backupSource, 
                RelativeRoot = relativeRoot, 
                OutputType = theOutputType,
                Output = output
            });

            program.Execute();
        }


        void Execute()
        {
            _resultWriter.Initialize();

            BackupFiles();

            _resultWriter.Complete();
        }


        void BackupFiles()
        {
            var files = _searcher.FindFiles(_opts.BackupSource);

            // try to leave a couple threads available for the GC
            var opts = new ParallelOptions { MaxDegreeOfParallelism = _vpus };

            Parallel.ForEach(files, opts, BackupFile);
        }


        void BackupFile(string file)
        {
            var backupFile = new BackupFile(file, _opts.RelativeRoot);
            
            var result = new BackupResult {
                Region = _opts.Region,
                Vault = _opts.VaultName,
                Backup = backupFile
            };
            
            for(var i = 1; i <= RETRY_COUNT; i++)
            {
                var attempt = i > 1 ? $" (attempt {i})" : string.Empty;

                try
                {
                    Console.WriteLine($"  - backing up {backupFile.GlacierDescription}{attempt}");

                    result.Result = _atm.UploadAsync(_opts.VaultName, backupFile.GlacierDescription, backupFile.FullPath).Result;

                    lock(_lockObj)
                    {
                        _resultWriter.WriteResult(result);
                    }

                    return;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"  - error backing up {backupFile.GlacierDescription}{attempt}: {ex.Message}");

                    var ms = _rand.Next(START_WAIT_TIME_MS) * i;  // wait for a random amount of time, and increase as the number of tries increases

                    Thread.Sleep(ms);
                }
            }

            Console.WriteLine($" ** unable to backup {backupFile.GlacierDescription} **");
        }


        IFileSearcher GetFileSearcher()
        {
            switch(_opts.BackupType)
            {
                case BackupType.Assets:
                {
                    return new AssetFileSearcher();
                }
                case BackupType.Full:
                {
                    return new AllFileSearcher();
                }
                case BackupType.File:
                {
                    return new SingleFileSearcher();
                }
                case BackupType.List:
                {
                    return new ListFileSearcher();
                }
            }

            throw new InvalidOperationException("This backup type is not properly supported");
        }


        IResultWriter GetResultWriter()
        {
            switch(_opts.OutputType)
            {
                case OutputType.PhotoSql:
                    return new PhotosPgSqlResultWriter(_opts.Output);
                case OutputType.VideoSql:
                    return new VideosPgSqlResultWriter(_opts.Output);
                case OutputType.Csv:
                    return new CsvResultWriter(_opts.Output);
            }

            throw new InvalidOperationException("This output type is not properly supported");
        }


        int GetVpus()
        {
            return Math.Max(1, Environment.ProcessorCount - 1);
        }


        static void ShowUsage()
        {
            Console.WriteLine("GlacierBackup <aws_profile> <aws_region> <aws_vault> <backup_type> <backup_source> <relative_root> <output_type> <output_file>");
            Console.WriteLine("  where:");
            Console.WriteLine("    aws_profile = name of AWS profile from your credentials file");
            Console.WriteLine("    aws_region = name of AWS region (i.e. us-east-1, us-west-2)");
            Console.WriteLine("    aws_vault = name of the already created vault to store archives in");
            Console.WriteLine("    backup_type = one of the following:");
            Console.WriteLine("        assets: backup all files contained in 'src' directories ");
            Console.WriteLine("        full: backup all files in the specified directory or below");
            Console.WriteLine("        file: backup an individual file");
            Console.WriteLine("        list: backup all files contained in the specified file (1 per line)");
            Console.WriteLine("    backup_source = file or directory containing files to backup");
            Console.WriteLine("    relative_root = starting part of path to remove when building Glacier description");
            Console.WriteLine("    output_type = type of file to generate: [PhotoSql, VideoSql, Csv]");
            Console.WriteLine("        photosql: sql update script for photos");
            Console.WriteLine("        videosql: sql update script for videos");
            Console.WriteLine("        csv: generic CSV file");
            Console.WriteLine("    output_file = path where the sql should be written that maps archive details to the asset");
            Console.WriteLine();
        }
    }
}
