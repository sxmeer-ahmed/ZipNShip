using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ZipNShip.Core.Abstractions;

namespace ZipNShip.AzureBlob.Providers
{
    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _container;

        public AzureBlobStorageProvider(string connectionString, string containerName)
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }

        public async Task UploadAsync(Stream zipStream, string blobName, CancellationToken ct = default)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: ct);
            zipStream.Position = 0;
            await _container
                .GetBlobClient(blobName)
                .UploadAsync(zipStream, overwrite: true, cancellationToken: ct);
        }
    }
}
