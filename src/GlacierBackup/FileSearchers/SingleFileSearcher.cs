using System.Collections.Generic;

namespace GlacierBackup.FileSearchers;

public class SingleFileSearcher
    : IFileSearcher
{
    public IEnumerable<string> FindFiles(string file)
    {
        yield return file;
    }
}
