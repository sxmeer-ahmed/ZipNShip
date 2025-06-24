using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public class AzureIndexStore : IIndexStore
    {
        private readonly TableClient _table;

        public AzureIndexStore(string connectionString, string tableName)
        {
            _table = new TableClient(connectionString, tableName);
        }

        public async Task<string> GetZipFileNameAsync(string fileName, CancellationToken ct = default)
        {
            await _table.CreateIfNotExistsAsync(ct);

            try
            {
                Response<TableEntity> resp =
                    await _table.GetEntityAsync<TableEntity>(
                        partitionKey: fileName,
                        rowKey: fileName,
                        cancellationToken: ct);

                return resp.Value.GetString("ZipFileName");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task SaveFileMappingsAsync(ZipNShipFile zipNShipFile, string zipFileName = null, CancellationToken ct = default)
        {
            await SaveFileMappingsAsync(zipNShipFile.fileNames.Keys.ToList(), zipFileName, ct);
        }
        public async Task SaveFileMappingsAsync(List<string> FileNames, string zipFileName = null, CancellationToken ct = default)
        {
            await _table.CreateIfNotExistsAsync(ct);
            zipFileName = zipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            var batch = new List<TableTransactionAction>();
            foreach (string name in FileNames)
            {
                var entity = new TableEntity(name, name) { { "ZipFileName", zipFileName } };
                batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, entity));
            }
            await _table.SubmitTransactionAsync(batch);
            batch.Clear();
        }

    }
}
