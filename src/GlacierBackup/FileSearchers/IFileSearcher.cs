using System.Collections.Generic;


namespace GlacierBackup.FileSearchers;

public interface IFileSearcher
{
    IEnumerable<string> FindFiles(string rootDirectory);
}
