using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZipNShip.Core
{
    public interface IIndexStore
    {
        Task SaveFileMappingsAsync(ZipNShipFile zipNShipFile, string zipFileName, CancellationToken ct = default);
        Task SaveFileMappingsAsync(List<string> FileNames, string zipFileName, CancellationToken ct = default);
        Task<string> GetZipFileNameAsync(string fileName, CancellationToken ct = default);
    }
}
