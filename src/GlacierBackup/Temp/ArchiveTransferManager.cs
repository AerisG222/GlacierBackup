using System;
using System.IO;
using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Runtime;


// TEMP: this should eventually become a real class in the AWS SDK - try to mirror the api calls to simplify
//       migrating to the official bits once that work is completed
//  ref: https://github.com/aws/aws-sdk-net/blob/netcore-development/sdk/src/Services/Glacier/Custom/_bcl/Transfer/ArchiveTransferManager.cs
namespace GlacierBackup.Temp
{
    class ArchiveTransferManager
    {
        AmazonGlacierClient _client;


        public ArchiveTransferManager(AWSCredentials credentials, RegionEndpoint region)
        {
            if(credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }

            if(region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            _client = new AmazonGlacierClient(credentials, region);
        }


        public UploadResult Upload(string vaultName, string archiveDescription, string filepath)
        {
            // reference: https://github.com/aws/aws-sdk-net/blob/netcore-development/sdk/src/Services/Glacier/Custom/_bcl/Transfer/Internal/SinglepartUploadCommand.cs
            var input = File.OpenRead(filepath);
            var checksum = TreeHashGenerator.CalculateTreeHash(input);

            try
            {
                var uploadRequest = new UploadArchiveRequest()
                {
                    AccountId = "-",
                    ArchiveDescription = archiveDescription,
                    VaultName = vaultName,
                    Checksum = checksum,
                    Body = input
                };

                //((Amazon.Runtime.Internal.IAmazonWebServiceRequest)uploadRequest).AddBeforeRequestHandler(new UserAgentPostFix("SingleUpload").UserAgentRequestEventHandlerSync);

                var response = _client.UploadArchive(uploadRequest);

                return new UploadResult {
                    ArchiveId = response.ArchiveId, 
                    Checksum = checksum
                };
            }
            finally
            {
                try { input.Close(); }
                catch (Exception) { }
            }
        }
    }
}
