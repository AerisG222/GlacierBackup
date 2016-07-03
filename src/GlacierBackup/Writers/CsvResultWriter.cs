using System;
using System.IO;
using CsvHelper;


namespace GlacierBackup.Writers
{
    public class CsvResultWriter
        : IResultWriter
    {
        string _outputPath;
        StreamWriter _writer;
        CsvWriter _csv;


        public CsvResultWriter(string outputPath)
        {
            _outputPath = outputPath;
        }


        public void Initialize()
        {
            _writer = new StreamWriter(new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, FileOptions.None));
            _csv = new CsvWriter(_writer);

            _csv.WriteField("region");
            _csv.WriteField("vault_name");
            _csv.WriteField("file_path");
            _csv.WriteField("glacier_description");
            _csv.WriteField("archive_id");
            _csv.WriteField("treehash");

            _csv.NextRecord();
        }
        

        public void WriteResult(BackupResult result)
        {
            _csv.WriteField(result.Region.SystemName);
            _csv.WriteField(result.Vault);
            _csv.WriteField(result.Backup.FullPath);
            _csv.WriteField(result.Backup.GlacierDescription);
            _csv.WriteField(result.Result.ArchiveId);
            _csv.WriteField(result.Result.Checksum);

            _csv.NextRecord();
        }


        public void Complete()
        {
            if(_csv != null)
            {
                _csv.Dispose();
            }
            
            if(_writer != null)
            {
                _writer.Dispose();
            }
        }
    } 
}
