using AllocServer.Interfaces.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AllocServer.Services.Storage
{
    public class AzureBlobStorageStrategy : IStorageStrategy
    {
        private readonly IConfiguration _configuration;

        public AzureBlobStorageStrategy(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string contentType,
            string blobPath)
        {
            var containerClient = GetContainerClient();
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var blobClient = containerClient.GetBlobClient(blobPath);
            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            return blobPath;
        }

        public Task<string> GetPresignedUrlAsync(
            string blobPath,
            TimeSpan expiration,
            string? downloadFileName = null)
        {
            var blobClient = GetContainerClient().GetBlobClient(blobPath);
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string must include an account key to generate SAS URLs.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = GetContainerName(),
                BlobName = blobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var sanitized = System.Text.RegularExpressions.Regex.Replace(downloadFileName, @"[^a-zA-Z0-9\.\-_]", "_");
                var encoded = Uri.EscapeDataString(downloadFileName);
                sasBuilder.ContentDisposition = $"attachment; filename=\"{sanitized}\"; filename*=UTF-8''{encoded}";
            }

            return Task.FromResult(blobClient.GenerateSasUri(sasBuilder).ToString());
        }

        public async Task DeleteFileAsync(string blobPath)
        {
            var blobClient = GetContainerClient().GetBlobClient(blobPath);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

        private BlobContainerClient GetContainerClient()
        {
            var connectionString = _configuration["Storage:AzureBlob:ConnectionString"]
                                   ?? _configuration.GetConnectionString("AzureBlobStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage connection string is missing.");
            }

            return new BlobServiceClient(connectionString).GetBlobContainerClient(GetContainerName());
        }

        private string GetContainerName()
        {
            var containerName = _configuration["Storage:AzureBlob:ContainerName"];
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException("Azure Blob Storage container name is missing.");
            }

            return containerName;
        }
    }
}
