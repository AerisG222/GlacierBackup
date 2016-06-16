using System;
using System.Collections.Generic;
using System.IO;


namespace GlacierBackup.FileSearchers
{
    public class AllFileSearcher
        : IFileSearcher
    {
        public IEnumerable<string> FindFiles(string rootDirectory)
        {
            return Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories);
        }
    }
}
