using AllocServer.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AllocServer.Services.Storage
{
    public class StorageFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StorageFactory(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public IStorageStrategy Create()
        {
            var provider = _configuration["Storage:Provider"] ?? "AzureBlob";

            return provider.Trim().ToUpperInvariant() switch
            {
                "AZURE" or "AZUREBLOB" or "AZURE_BLOB" => 
                    _serviceProvider.GetRequiredService<AzureBlobStorageStrategy>(),
                "S3" or "SUPABASE" => 
                    _serviceProvider.GetRequiredService<S3StorageStrategy>(),
                _ => throw new InvalidOperationException("Storage provider khong duoc ho tro.")
            };
        }
    }
}
