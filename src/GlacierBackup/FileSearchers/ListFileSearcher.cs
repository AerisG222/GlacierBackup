using System;
using System.Collections.Generic;
using System.IO;


namespace GlacierBackup.FileSearchers
{
    public class ListFileSearcher
        : IFileSearcher
    {
        public IEnumerable<string> FindFiles(string sourceFile)
        {
            using(var sr = new StreamReader(sourceFile))
            {
                while(sr.Peek() > 0)
                {
                    var line = sr.ReadLine();

                    if(!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
