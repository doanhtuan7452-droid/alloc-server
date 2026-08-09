using Amazon.S3;
using Amazon.S3.Model;
using AllocServer.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AllocServer.Services.Storage
{
    public class S3StorageStrategy : IStorageStrategy
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public S3StorageStrategy(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string contentType,
            string blobPath)
        {
            var bucketName = GetBucketName();

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = blobPath,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(putRequest);

            return blobPath;
        }

        public Task<string> GetPresignedUrlAsync(
            string blobPath,
            TimeSpan expiration,
            string? downloadFileName = null)
        {
            var bucketName = GetBucketName();

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = blobPath,
                Expires = DateTime.UtcNow.Add(expiration)
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var sanitized = System.Text.RegularExpressions.Regex.Replace(downloadFileName, @"[^a-zA-Z0-9\.\-_]", "_");
                var encoded = Uri.EscapeDataString(downloadFileName);
                request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{sanitized}\"; filename*=UTF-8''{encoded}";
            }

            string url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public async Task DeleteFileAsync(string blobPath)
        {
            var bucketName = GetBucketName();
            await _s3Client.DeleteObjectAsync(bucketName, blobPath);
        }

        private string GetBucketName()
        {
            var bucketName = _configuration["Storage:S3:BucketName"];
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException("S3 Storage bucket name is missing.");
            }

            return bucketName;
        }
    }
}
