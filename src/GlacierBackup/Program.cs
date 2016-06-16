using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using GlacierBackup.FileSearchers;
using GlacierBackup.Writers;


namespace GlacierBackup
{
    public class Program
    {
        static readonly object _lockObj = new object();

        readonly RegionEndpoint _region;
        readonly string _vaultName;
        readonly BackupType _backupType;
        readonly string _backupSource;
        readonly string _relativeRoot;
        readonly IResultWriter _resultWriter;
        readonly IFileSearcher _searcher;
        readonly int _vpus;


        internal Program(RegionEndpoint region, string vaultName, BackupType backupType, string backupSource, string relativeRoot, string output)
        {
            _region = region;
            _vaultName = vaultName;
            _backupType = backupType;
            _backupSource = backupSource;
            _relativeRoot = relativeRoot;

            if(_backupType == BackupType.Assets)
            {
                _searcher = new AssetFileSearcher();
            }
            else
            {
                _searcher = new AllFileSearcher();
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
            if(args.Length != 6)
            {
                ShowUsage();
                Environment.Exit(1);
            }

            var awsRegion = RegionEndpoint.GetBySystemName(args[0]);
            var vaultName = args[1];
            var backupType = string.Equals(args[2], "assets", StringComparison.OrdinalIgnoreCase) ? BackupType.Assets : BackupType.Full;
            var backupSource = args[3];  // individual file / or folder
            var relativeRoot = args[4];
            var output = args[5];  // sql file to write

            if(File.Exists(output))
            {
                Console.WriteLine("Output file already exists - exiting!");
                Environment.Exit(3);
            }

            var program = new Program(awsRegion, vaultName, backupType, backupSource, relativeRoot, output);
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

            var result = new BackupResult {
                Region = _region,
                Vault = _vaultName,
                Backup = backupFile
            };

            lock(_lockObj)
            {
                _resultWriter.WriteResult(result);
            }
        }


        static void ShowUsage()
        {
            Console.WriteLine("GlacierBackup <aws_region> <aws_vault> <backup_type> <backup_source> <relative_root> <output_file>");
            Console.WriteLine("  where:");
            Console.WriteLine("     aws_region = name of AWS region (i.e. us-east-1, us-west-2)");
            Console.WriteLine("     aws_vault = name of the already created vault to store archives in");
            Console.WriteLine("     backup_type = 'assets' to only backup files contained in 'src' directories, ");
            Console.WriteLine("                   otherwise all files will be backed up");
            Console.WriteLine("     backup_source = file or directory containing files to backup");
            Console.WriteLine("     relative_root = starting part of path to remove when building Glacier description");
            Console.WriteLine("     output_file = path where the sql should be written that maps archive details to the asset");
            Console.WriteLine();
        }
    }
}
