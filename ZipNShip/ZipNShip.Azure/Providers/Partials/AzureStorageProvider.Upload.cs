using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public partial class AzureStorageProvider
    {
        public async Task<string> UploadAsync(ZipNShipFile ZipnShipFile, string ZipFileName = null, CancellationToken ct = default)
        {
            ZipnShipFile.FinalizeZip();
            return await UploadAsync(ZipnShipFile.GetZipStream(), ZipnShipFile.fileNames, ZipFileName, ct);
        }
        public string Upload(ZipNShipFile ZipnShipFile, string ZipFileName = null, CancellationToken ct = default)
        {
            ZipnShipFile.FinalizeZip();
            return Upload(ZipnShipFile.GetZipStream(), ZipnShipFile.fileNames, ZipFileName, ct);
        }
        public async Task<string> UploadAsync(MemoryStream ZipStream, List<string> FileNames, string ZipFileName = null, CancellationToken ct = default)
        {
            ZipFileName = ZipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            await _container.CreateIfNotExistsAsync(cancellationToken: ct);
            await _container
                .GetBlobClient(ZipFileName)
                .UploadAsync(ZipStream, overwrite: true, cancellationToken: ct);
            await _indexStore.SaveFileMappingsAsync(FileNames, ZipFileName);
            return ZipFileName;
        }
        public string Upload(MemoryStream ZipStream, List<string> FileNames, string ZipFileName = null, CancellationToken ct = default)
        {
            ZipFileName = ZipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            _container.CreateIfNotExists(cancellationToken: ct);
            _container
                .GetBlobClient(ZipFileName)
                .Upload(ZipStream, overwrite: true, cancellationToken: ct);
            _indexStore.SaveFileMappings(FileNames, ZipFileName);
            return ZipFileName;
        }
    }
}
