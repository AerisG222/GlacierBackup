using System.Collections.Generic;

namespace GlacierBackup.Writers;

public interface IResultWriter
{
    void WriteResults(IEnumerable<BackupResult> results);
}
