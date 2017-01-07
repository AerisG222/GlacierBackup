namespace GlacierBackup.Writers
{
    public interface IResultWriter
    {
        void Initialize();
        void WriteResult(BackupResult result);
        void Complete();
    } 
}
