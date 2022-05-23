namespace GlacierBackup.Writers;

public class VideosPgSqlResultWriter
    : PgSqlResultWriter
{
    public VideosPgSqlResultWriter(string outputPath)
    {
        _outputPath = outputPath;
    }


    public override void WriteResult(BackupResult result)
    {
        _writer.WriteLine($"    UPDATE video.video ");
        _writer.WriteLine($"       SET aws_glacier_vault_id = (SELECT id FROM aws.glacier_vault WHERE region = '{result.Region.SystemName}' AND vault_name = '{result.Vault}'),");
        _writer.WriteLine($"           aws_archive_id = '{result.Result?.ArchiveId}',");
        _writer.WriteLine($"           aws_treehash = '{result.Result?.Checksum}'");
        _writer.WriteLine($"     WHERE raw_path = '/movies/{result.Backup.GlacierDescription}';");
        _writer.WriteLine();
    }
}
