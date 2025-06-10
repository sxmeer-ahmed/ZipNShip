using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using ZipNShip.Core.Abstractions;

namespace ZipNShip.AmazonS3.Providers
{
    public class AmazonS3StorageProvider : IStorageProvider
    {
        private readonly IAmazonS3 _client;
        private readonly string    _bucket;

        public AmazonS3StorageProvider(IAmazonS3 client, string bucketName)
        {
            _client = client;
            _bucket = bucketName;
        }

        public async Task UploadAsync(Stream zipStream, string blobName, CancellationToken ct = default)
        {
            var req = new PutObjectRequest
            {
                BucketName  = _bucket,
                Key         = blobName,
                InputStream = zipStream
            };
            await _client.PutObjectAsync(req, ct);
        }
    }
}
