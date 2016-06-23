using System;
using Amazon;
using GlacierBackup.Temp;


namespace GlacierBackup
{
    public class BackupResult
    {
        public RegionEndpoint Region { get; set; }
        public string Vault { get; set; }
        public BackupFile Backup { get; set; }
        public UploadResult Result { get; set; }
    }
}
