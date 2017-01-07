namespace GlacierBackup
{
    public class BackupFile
    {
        public string FullPath { get; private set; }
        public string RelativeRoot { get; private set; }


        public string GlacierDescription 
        { 
            get
            {
                return FullPath.Replace(RelativeRoot, string.Empty);
            }
        }


        public BackupFile(string fullPath, string relativeRoot)
        {
            FullPath = fullPath;
            RelativeRoot = relativeRoot;
        }
    }
}
