namespace AllocServer.Interfaces.Storage
{
    public interface IStorageStrategy
    {
        Task<string> UploadFileAsync(
            Stream fileStream,
            string contentType,
            string blobPath);

        Task<string> GetPresignedUrlAsync(
            string blobPath,
            TimeSpan expiration,
            string? downloadFileName = null);

        Task DeleteFileAsync(string blobPath);
    }
}
