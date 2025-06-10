using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZipNShip.Core.Abstractions
{
    public interface IIndexStore
    {
        Task SaveFileMappingsAsync(string zipName, IEnumerable<string> fileNames, CancellationToken ct = default);
    }
}
