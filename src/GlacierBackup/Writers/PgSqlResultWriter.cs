using System.IO;


namespace GlacierBackup.Writers;

public abstract class PgSqlResultWriter
    : IResultWriter
{
    protected string _outputPath;
    protected StreamWriter _writer;


    public virtual void Initialize()
    {
        _writer = new StreamWriter(new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, FileOptions.None));

        _writer.WriteLine("DO");
        _writer.WriteLine("$$");
        _writer.WriteLine("BEGIN");
        _writer.WriteLine();
    }


    public virtual void Complete()
    {
        _writer.WriteLine();
        _writer.WriteLine("END");
        _writer.WriteLine("$$");

        _writer.Flush();
        _writer.Dispose();
        _writer = null;
    }


    public abstract void WriteResult(BackupResult result);
} 
