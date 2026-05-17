using AllocServer.Interfaces.Storage;

namespace AllocServer.Services.Storage
{
    public class StorageFactory
    {
        private readonly IConfiguration _configuration;
        private readonly AzureBlobStorageStrategy _azureBlobStorageStrategy;

        public StorageFactory(
            IConfiguration configuration,
            AzureBlobStorageStrategy azureBlobStorageStrategy)
        {
            _configuration = configuration;
            _azureBlobStorageStrategy = azureBlobStorageStrategy;
        }

        public IStorageStrategy Create()
        {
            var provider = _configuration["Storage:Provider"] ?? "AzureBlob";

            return provider.Trim().ToUpperInvariant() switch
            {
                "AZURE" or "AZUREBLOB" or "AZURE_BLOB" => _azureBlobStorageStrategy,
                _ => throw new InvalidOperationException("Storage provider khong duoc ho tro.")
            };
        }
    }
}
