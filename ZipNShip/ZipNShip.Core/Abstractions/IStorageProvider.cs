using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZipNShip.Core
{
    public interface IStorageProvider
    {
        Task<string> UploadAsync(ZipNShipFile ZipNShipFile, string ZipFileName = null, CancellationToken ct = default);
        Task<string> UploadAsync(MemoryStream ZipStream, List<string> FileNames, string ZipFileName = null, CancellationToken ct = default);
    }
}
