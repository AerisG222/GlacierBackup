using System;
using System.Collections.Generic;
using System.IO;


namespace GlacierBackup.FileSearchers
{
    public class SingleFileSearcher
        : IFileSearcher
    {
        public IEnumerable<string> FindFiles(string file)
        {
            yield return file;
        }
    }
}
