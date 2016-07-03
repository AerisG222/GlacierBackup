using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Glacier.Transfer;
using Amazon.Runtime;
using GlacierBackup.FileSearchers;
using GlacierBackup.Writers;


namespace GlacierBackup
{
    public class Program
    {
        static readonly object _lockObj = new object();

        readonly AWSCredentials _credentials;
        readonly RegionEndpoint _region;
        readonly string _vaultName;
        readonly BackupType _backupType;
        readonly string _backupSource;
        readonly string _relativeRoot;
        readonly IResultWriter _resultWriter;
        readonly IFileSearcher _searcher;
        readonly int _vpus;


        internal Program(AWSCredentials credentials, RegionEndpoint region, string vaultName, BackupType backupType, string backupSource, string relativeRoot, string output)
        {
            _credentials = credentials;
            _region = region;
            _vaultName = vaultName;
            _backupType = backupType;
            _backupSource = backupSource;
            _relativeRoot = relativeRoot;

            switch(_backupType)
            {
                case BackupType.Assets:
                {
                    _searcher = new AssetFileSearcher();
                    break;
                }
                case BackupType.Full:
                {
                    _searcher = new AllFileSearcher();
                    break;
                }
                case BackupType.File:
                {
                    _searcher = new SingleFileSearcher();
                    break;
                }
            }

            _vpus = Environment.ProcessorCount - 1;

            if(_vpus < 1)
            {
                _vpus = 1;
            }

            _resultWriter = new SqlResultWriter(output);
        }


        public static void Main(string[] args)
        {
            if(args.Length != 7)
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
            var output = args[6];  // sql file to write

            AWSCredentials credentials = null;

            try
            {
                credentials = new StoredProfileAWSCredentials(profile, StoredProfileCredentials.DefaultSharedCredentialLocation);
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

            if(!Enum.IsDefined(typeof(BackupType), backupType))
            {
                Console.WriteLine($"Please specify a valid backup type [Full, Assets, File].");
                Environment.Exit(2);
            }

            var theBackupType = (BackupType)Enum.Parse(typeof(BackupType), backupType);

            if(theBackupType == BackupType.File && !File.Exists(backupSource))
            {
                Console.WriteLine($"The specified backup file [{backupSource}] does not exist.  Please enter a valid directory path to backup.");
                Environment.Exit(2);
            }
            else if(theBackupType != BackupType.File && !Directory.Exists(backupSource))
            {
                Console.WriteLine($"The specified backup directory [{backupSource}] does not exist.  Please enter a valid directory path to backup.");
                Environment.Exit(2);
            }

            if(relativeRoot.Length > backupSource.Length)
            {
                Console.WriteLine("The relative_root path should be the starting part of the path to the backup to remove, such that the remaining path is tracked as the archive description.");
                Environment.Exit(2);
            }

            if(!backupSource.StartsWith(relativeRoot))
            {
                Console.WriteLine("The relative_root should exactly match the same starting path to the backup_source.");
                Environment.Exit(2);
            }

            if(File.Exists(output))
            {
                Console.WriteLine("Output file already exists - exiting!");
                Environment.Exit(2);
            }

            var program = new Program(credentials, awsRegion, vaultName, theBackupType, backupSource, relativeRoot, output);

            program.Execute();
        }


        void Execute()
        {
            _resultWriter.Initialize();

            if(File.Exists(_backupSource))
            {
                BackupFile(_backupSource);
            }
            else if(Directory.Exists(_backupSource))
            {
                BackupDirectory(_backupSource);
            }
            else
            {
                Console.WriteLine("Did not find a valid file or directory to backup!");
                Environment.Exit(2);
            }

            _resultWriter.Complete();
        }


        void BackupDirectory(string directory)
        {
            var files = _searcher.FindFiles(directory);

            // try to leave a couple threads available for the GC
            var opts = new ParallelOptions { MaxDegreeOfParallelism = _vpus };

            Parallel.ForEach(files, opts, BackupFile);
        }


        void BackupFile(string file)
        {
            var backupFile = new BackupFile(file, _relativeRoot);
            var atm = new ArchiveTransferManager(_credentials, _region);
            var result = new BackupResult {
                Region = _region,
                Vault = _vaultName,
                Backup = backupFile
            };
            
            Console.WriteLine($"  - backing up {backupFile.GlacierDescription}...");

            result.Result = atm.Upload(_vaultName, backupFile.GlacierDescription, backupFile.FullPath);

            lock(_lockObj)
            {
                _resultWriter.WriteResult(result);
            }
        }


        static void ShowUsage()
        {
            Console.WriteLine("GlacierBackup <aws_region> <aws_vault> <backup_type> <backup_source> <relative_root> <output_file>");
            Console.WriteLine("  where:");
            Console.WriteLine("    aws_profile = name of AWS profile from your credentials file");
            Console.WriteLine("    aws_region = name of AWS region (i.e. us-east-1, us-west-2)");
            Console.WriteLine("    aws_vault = name of the already created vault to store archives in");
            Console.WriteLine("    backup_type = one of the following:");
            Console.WriteLine("        assets: backup all files contained in 'src' directories ");
            Console.WriteLine("        full: backup all files in the specified directory or below");
            Console.WriteLine("        file: backup an individual file");
            Console.WriteLine("    backup_source = file or directory containing files to backup");
            Console.WriteLine("    relative_root = starting part of path to remove when building Glacier description");
            Console.WriteLine("    output_file = path where the sql should be written that maps archive details to the asset");
            Console.WriteLine();
        }
    }
}
