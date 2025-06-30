using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public class AzureStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _container;
        private readonly IIndexStore _indexStore;
        private AzureStorageProvider(string connectionString, string containerName, IIndexStore indexStore, string tableName)
        {
            _container = new BlobContainerClient(connectionString, containerName);

            if (indexStore != null)
            {
                _indexStore = indexStore;
            }
            else
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ArgumentException("Table name must be provided when using default Azure table index.");

                _indexStore = new AzureIndexStore(connectionString, tableName);
            }
        }

        // 
        // Summary:
        //       Initializes AzureStorageProvider with Default Azure Table
        //
        // Parameters:
        //      ConnectionString:
        //          Azure Blob Connection String
        //
        //      ContainerName:
        //          Blob Container Name
        //
        //      AzureTableName:
        //          Azure Table Name to Store File-to-Zip Name Mapping
        public AzureStorageProvider(string ConnectionString, string ContainerName, string AzureTableName)
            : this(ConnectionString, ContainerName, null, AzureTableName) { }

        // 
        // Summary:
        //       Initializes AzureStorageProvider with Default Azure Table
        //
        // Parameters:
        //      ConnectionString:
        //          Azure Blob Connection String
        //
        //      ContainerName:
        //          Blob Container Name
        //
        //      IndexStore:
        //          Custom Index Store for File-to-Zip Name Mapping
        public AzureStorageProvider(string ConnectionString, string ContainerName, IIndexStore IndexStore)
            : this(ConnectionString, ContainerName, IndexStore, null) { }
        public async Task<string> UploadAsync(ZipNShipFile ZipnShipFile, string ZipFileName = null, CancellationToken ct = default)
        {
            ZipnShipFile.FinalizeZip();
            return await UploadAsync(ZipnShipFile.GetZipStream(),ZipnShipFile.fileNames, ZipFileName, ct);
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
        public async Task DownloadAsync(string fileName, string downloadPath, CancellationToken ct = default)
        {
            string zipFileName = await _indexStore.GetZipFileNameAsync(fileName, ct) ?? throw new FileNotFoundException();
            var zipStream = new MemoryStream();
            await _container
                .GetBlobClient(zipFileName)
                .DownloadToAsync(zipStream, ct);

            zipStream.Position = 0;

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entry = archive.GetEntry(fileName)
                    ?? throw new FileNotFoundException(
                        $"The ZIP '{zipFileName}' does not contain an entry named '{fileName}'.");

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
    }
}
