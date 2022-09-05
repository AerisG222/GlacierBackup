namespace GlacierBackup.Writers;

public abstract class PgSqlResultWriter
    : BaseResultWriter
{
    public PgSqlResultWriter(string outputPath)
        : base(outputPath)
    {

    }

    protected override void Initialize()
    {
        _writer.WriteLine("DO");
        _writer.WriteLine("$$");
        _writer.WriteLine("BEGIN");
        _writer.WriteLine();
    }

    protected override void Complete()
    {
        _writer.WriteLine();
        _writer.WriteLine("END");
        _writer.WriteLine("$$");
    }
}
