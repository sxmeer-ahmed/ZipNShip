using System.IO.Compression;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using ZipNShip.Core;
using static ZipNShip.Core.Enums;

namespace ZipNShip.AzureBlob;
public class AzureBlobStorageProvider : IStorageProvider,IIndexStore
{
    private readonly BlobContainerClient _container;
    private readonly TableClient _table;

    public AzureBlobStorageProvider(string connectionString, string containerName,IndexStoreType indexStoreType, IIndexStore indexStore, string tableName = null)
    {
        _container = new BlobContainerClient(connectionString, containerName);
        _table = String.IsNullOrEmpty(tableName) ? null : new TableClient(connectionString, tableName);
    }

    public async Task UploadAsync(ZipNShipFile zipnShipFile, string zipFileName, CancellationToken ct = default)
    {
        var zipStream = zipnShipFile.zipStream;
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        zipStream.Position = 0;
        await _container
            .GetBlobClient(zipFileName)
            .UploadAsync(zipStream, overwrite: true, cancellationToken: ct);
    }
    public async Task SaveFileMappingsAsync(ZipNShipFile zipNShipFile, string zipFileName, CancellationToken ct = default)
    {
        await _table.CreateIfNotExistsAsync();

        var batch = new List<TableTransactionAction>();
        foreach (string name in zipNShipFile.fileNames.Keys)
        {
            var entity = new TableEntity(name, name) { { "ZipFileName", zipFileName } };
            batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, entity));
        }
        await _table.SubmitTransactionAsync(batch);
        batch.Clear();
    }

    public async Task DownloadAsync(string fileName, string downloadPath, CancellationToken ct = default)
    {
        await _table.CreateIfNotExistsAsync(cancellationToken: ct);

        TableEntity entity;
        try
        {
            entity = await _table.GetEntityAsync<TableEntity>(
                partitionKey: fileName,
                rowKey: fileName,
                cancellationToken: ct
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException(
                $"No mapping found for file '{fileName}' in table '{_table.Name}'.", ex);
        }

        var zipFileName = entity.GetString("ZipFileName");
        if (string.IsNullOrEmpty(zipFileName))
            throw new InvalidOperationException($"Mapping for '{fileName}' has no ZipFileName.");

        var zipStream = new MemoryStream();
        await _container
            .GetBlobClient(zipFileName)
            .DownloadToAsync(zipStream, ct);

        zipStream.Position = 0;

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry(fileName)
            ?? throw new FileNotFoundException(
                $"The ZIP '{zipFileName}' does not contain an entry named '{fileName}'.");

        var dir = Path.GetDirectoryName(downloadPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var entryStream = entry.Open();
        using var fileStream = File.Create(downloadPath);
        await entryStream.CopyToAsync(fileStream, ct);
    }
}
