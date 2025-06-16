using Azure.Data.Tables;
using ZipNShip.Core;

namespace ZipNShip.AzureBlob;
public class AzureBlobIndexStore : IIndexStore
{
    private readonly TableClient _table;

    public AzureBlobIndexStore(string connectionString, string tableName )
    {
        _table = String.IsNullOrEmpty(tableName) ? null : new TableClient(connectionString, tableName);
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
}
