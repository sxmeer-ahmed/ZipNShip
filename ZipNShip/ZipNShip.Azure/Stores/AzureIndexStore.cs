using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
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
                int hash = fileName.GetHashCode();
                string partKey = (Math.Abs(hash) % 100).ToString("D2");

                Response<TableEntity> resp =
                    await _table.GetEntityAsync<TableEntity>(
                        partitionKey: partKey,
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
            await SaveFileMappingsAsync(zipNShipFile.fileNames, zipFileName, ct);
        }
        public async Task SaveFileMappingsAsync(List<string> FileNames, string zipFileName = null, CancellationToken ct = default)
        {
            await _table.CreateIfNotExistsAsync(ct);
            zipFileName = zipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            Dictionary<string, List<TableTransactionAction>> batches = new Dictionary<string, List<TableTransactionAction>>();
            foreach (string fileName in FileNames)
            {
                int hash = fileName.GetHashCode();
                string partitionKey = (Math.Abs(hash) % 100).ToString("D2");

                if (!batches.ContainsKey(partitionKey))
                {
                    batches.Add( partitionKey, new List<TableTransactionAction> { } );
                }

                batches[partitionKey].Add( new TableTransactionAction (
                    TableTransactionActionType.UpsertMerge, 
                    new TableEntity(partitionKey, fileName) { { "ZipFileName", zipFileName } } ) );

                if (batches[partitionKey].Count == 100)
                {
                    await _table.SubmitTransactionAsync(batches[partitionKey]);
                    batches[partitionKey].Clear();
                }
            }
            foreach (string partKey in batches.Keys)
            {
                await _table.SubmitTransactionAsync(batches[partKey]);
                batches[partKey].Clear();
            }
        }

    }
}
