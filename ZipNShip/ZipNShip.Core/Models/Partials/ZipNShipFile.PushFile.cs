using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ZipNShip.Core
{
    public partial class ZipNShipFile
    {
        public async Task<string> PushFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string fileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(filePath)}";
            fileNames.Add(fileName);

            using (var fileStream = File.OpenRead(filePath))
            {
                ulong fileSize = (ulong)fileStream.Length;

                if (currentSizeInBytes + fileSize > _maxSizeInBytes)
                {
                    SizeLimitReached?.Invoke(this, EventArgs.Empty);

                    if (_autoSplitStorageProvider != null)
                    {
                        FinalizeZip();
                        await _autoSplitStorageProvider.UploadAsync(zipStream, fileNames);
                        ResetZip();
                    }
                    else
                        throw new Exception("File size is too big. Increase memory or enable AutoSplit.");
                }

                ZipArchiveEntry entry = _zipArchive.CreateEntry(fileName);
                using (Stream entryStream = entry.Open())
                {
                   await fileStream.CopyToAsync(entryStream);
                }
                currentSizeInBytes += fileSize;
            }

            return fileName;
        }
        public string PushFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string fileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(filePath)}";
            fileNames.Add(fileName);

            using (var fileStream = File.OpenRead(filePath))
            {
                ulong fileSize = (ulong)fileStream.Length;

                if (currentSizeInBytes + fileSize > _maxSizeInBytes)
                {
                    SizeLimitReached?.Invoke(this, EventArgs.Empty);

                    if (_autoSplitStorageProvider != null)
                    {
                        FinalizeZip();
                        _autoSplitStorageProvider.Upload(zipStream, fileNames);
                        ResetZip();
                    }
                    else
                        throw new Exception("File size is too big. Increase memory or enable AutoSplit.");
                }

                ZipArchiveEntry entry = _zipArchive.CreateEntry(fileName);
                using (Stream entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }
                currentSizeInBytes += fileSize;
            }

            return fileName;
        }
    }
}
