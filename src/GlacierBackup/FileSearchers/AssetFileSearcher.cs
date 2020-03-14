using System;
using System.Collections.Generic;
using System.IO;


namespace GlacierBackup.FileSearchers
{
    public abstract class AssetFileSearcher
        : IFileSearcher
    {
        readonly string _srcDir;


        public AssetFileSearcher(string srcDir)
        {
            if(string.IsNullOrWhiteSpace(srcDir))
            {
                throw new ArgumentNullException(nameof(srcDir));
            }

            _srcDir = srcDir;
        }


        public IEnumerable<string> FindFiles(string rootDirectory)
        {
            var files = new List<string>();
            var di = new DirectoryInfo(rootDirectory);

            if(string.Equals(di.Name, _srcDir, StringComparison.OrdinalIgnoreCase))
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
