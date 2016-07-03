using System;
using Amazon;
using Amazon.Runtime;


namespace GlacierBackup
{
    public class Options
    {
        public AWSCredentials Credentials { get; set; } 
        public RegionEndpoint Region { get; set; }
        public string VaultName { get; set; }
        public BackupType BackupType { get; set; } 
        public string BackupSource { get; set; } 
        public string RelativeRoot { get; set; } 
        public OutputType OutputType { get; set; }
        public string Output { get; set; }
    }
}
