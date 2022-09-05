using System.Globalization;
using CsvHelper;

namespace GlacierBackup.Writers;

public class CsvResultWriter
    : BaseResultWriter
{
    CsvWriter _csv;

    public CsvResultWriter(string outputPath)
        : base(outputPath)
    {

    }

    protected override void Initialize()
    {
        _csv = new CsvWriter(_writer, CultureInfo.CurrentCulture);

        _csv.WriteField("region");
        _csv.WriteField("vault_name");
        _csv.WriteField("file_path");
        _csv.WriteField("glacier_description");
        _csv.WriteField("archive_id");
        _csv.WriteField("treehash");

        _csv.NextRecord();
    }

    protected override void WriteResult(BackupResult result)
    {
        _csv.WriteField(result.Region.SystemName);
        _csv.WriteField(result.Vault);
        _csv.WriteField(result.Backup.FullPath);
        _csv.WriteField(result.Backup.GlacierDescription);
        _csv.WriteField(result.Result.ArchiveId);
        _csv.WriteField(result.Result.Checksum);

        _csv.NextRecord();
    }

    protected override void Complete()
    {
        if (_csv != null)
        {
            _csv.Dispose();
        }
    }
}
