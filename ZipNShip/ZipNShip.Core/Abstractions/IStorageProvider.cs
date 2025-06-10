using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZipNShip.Core.Abstractions
{
    public interface IStorageProvider
    {
        Task UploadAsync(Stream zipStream, string blobName, CancellationToken ct = default);
    }
}
