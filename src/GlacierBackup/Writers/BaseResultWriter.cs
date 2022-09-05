using System;
using System.Collections.Generic;
using System.IO;

namespace GlacierBackup.Writers;

public abstract class BaseResultWriter
    : IResultWriter
{
    protected readonly string _outputPath;
    protected StreamWriter _writer;

    protected BaseResultWriter(string outputPath)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        _writer = new StreamWriter(new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, FileOptions.None));
    }

    public virtual void WriteResults(IEnumerable<BackupResult> backupResults)
    {
        Initialize();

        foreach(var result in backupResults)
        {
            WriteResult(result);
        }

        Complete();

        if(_writer != null)
        {
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }
    }

    protected abstract void Initialize();
    protected abstract void WriteResult(BackupResult br);
    protected abstract void Complete();
}
