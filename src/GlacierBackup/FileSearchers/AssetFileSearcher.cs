using System;
using System.Collections.Generic;
using System.IO;


namespace GlacierBackup.FileSearchers
{
    public class AssetFileSearcher
        : IFileSearcher
    {
        public IEnumerable<string> FindFiles(string rootDirectory)
        {
            var files = new List<string>();
            var di = new DirectoryInfo(rootDirectory);
            
            // if this is the src directory, return contained files underneath it
            if(string.Equals(di.Name, "src", StringComparison.OrdinalIgnoreCase))
            {
                return Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories);
            }
            
            // if not, traverse our child directories, seeking src folders
            foreach(var dir in di.EnumerateDirectories())
            {
                files.AddRange(FindFiles(dir.FullName));
            }

            return files;
        }
    }
}
