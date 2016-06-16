using System;
using System.IO;
using GlacierBackup;


namespace GlacierBackup.Writers
{
    public class SqlResultWriter
        : IResultWriter
    {
        readonly string _outputPath;
        StreamWriter _writer;


        public SqlResultWriter(string outputPath)
        {
            _outputPath = outputPath;
        }


        public void Initialize()
        {
            _writer = new StreamWriter(new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, FileOptions.None));
            
            _writer.WriteLine("DO");
            _writer.WriteLine("$$");
            _writer.WriteLine("BEGIN");
            _writer.WriteLine();
        }


        public void WriteResult(BackupResult result)
        {
            // TODO: find a decent way to emit custom sql based on the asset that is being archived
            if(string.Equals(result.Vault, "photos", StringComparison.OrdinalIgnoreCase))
            {
                _writer.WriteLine($"    UPDATE photo.photo ");
                _writer.WriteLine($"       SET aws_region = '{result.Region}',");
                _writer.WriteLine($"           aws_vault = '{result.Vault}',");
                _writer.WriteLine($"           aws_archive_id = 'result.Result.ArchiveId',");
                _writer.WriteLine($"           aws_treehash = 'result.Result.Checksum'");
                _writer.WriteLine($"     WHERE src_path = '/images/{result.Backup.GlacierDescription}';");
                _writer.WriteLine();
            }
            else
            {
                _writer.WriteLine($"{result.Backup.GlacierDescription} - id: id, hash: hash");
            }    
        }


        public void Complete()
        {
            _writer.WriteLine();
            _writer.WriteLine("END");
            _writer.WriteLine("$$");
            
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
    } 
}
