using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace ZipNShip.Azure
{
    public partial class AzureStorageProvider
    {
        public async Task DownloadFileAsync(string fileName, string downloadFolderPath, CancellationToken ct = default)
        {
            MemoryStream zipStream = await GetZipMemoryStreamAsync(fileName, ct);
            string downloadPath = GetDownloadPath(fileName, downloadFolderPath);

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entry = archive.GetEntry(fileName)
                    ?? throw new FileNotFoundException(
                        $"The Mapped-Zip does not contain an entry named '{fileName}'.");

                var dir = Path.GetDirectoryName(downloadPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (var entryStream = entry.Open())
                using (var fileStream = File.Create(downloadPath))
                {
                    await entryStream.CopyToAsync(fileStream, 81920);
                }
            }
        }
        public void DownloadFile(string fileName, string downloadFolderPath, CancellationToken ct = default)
        {
            MemoryStream zipStream = GetZipMemoryStream(fileName, ct);
            string downloadPath = GetDownloadPath(fileName, downloadFolderPath);

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entry = archive.GetEntry(fileName)
                    ?? throw new FileNotFoundException(
                        $"The Mapped-Zip does not contain an entry named '{fileName}'.");

                var dir = Path.GetDirectoryName(downloadPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (var entryStream = entry.Open())
                using (var fileStream = File.Create(downloadPath))
                {
                    entryStream.CopyTo(fileStream, 81920);
                }
            }
        }
        private async Task<MemoryStream> GetZipMemoryStreamAsync(string fileName, CancellationToken ct)
        {
            string zipFileName = await _indexStore.GetZipFileNameAsync(fileName, ct) ?? throw new FileNotFoundException();
            MemoryStream zipStream = new MemoryStream();
            await _container
                .GetBlobClient(zipFileName)
                .DownloadToAsync(zipStream, ct);
            zipStream.Position = 0;
            return zipStream;
        }
        private MemoryStream GetZipMemoryStream(string fileName, CancellationToken ct)
        {
            string zipFileName = _indexStore.GetZipFileName(fileName, ct) ?? throw new FileNotFoundException();
            MemoryStream zipStream = new MemoryStream();
             _container
                .GetBlobClient(zipFileName)
                .DownloadTo(zipStream, ct);
            zipStream.Position = 0;
            return zipStream;
        }
        private string GetDownloadPath(string fileName, string downloadFolderPath)
        {
            string originalFileName = fileName.Split('_')[2];

            if (Path.HasExtension(downloadFolderPath))
            {
                downloadFolderPath = Path.GetDirectoryName(downloadFolderPath);
            }

            if (string.IsNullOrEmpty(downloadFolderPath))
                downloadFolderPath = Directory.GetCurrentDirectory();

            downloadFolderPath = downloadFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string finalPath = Path.Combine(downloadFolderPath, originalFileName);

            finalPath = Path.GetFullPath(finalPath);

            return finalPath;
        }
    }
}
