using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public partial class AzureIndexStore
    {
        public async Task SaveFileMappingsAsync(ZipNShipFile zipNShipFile, string zipFileName = null, CancellationToken ct = default)
        {
            await SaveFileMappingsAsync(zipNShipFile.fileNames, zipFileName, ct);
        }
        public void SaveFileMappings(ZipNShipFile zipNShipFile, string zipFileName = null, CancellationToken ct = default)
        {
            SaveFileMappings(zipNShipFile.fileNames, zipFileName, ct);
        }
        public void SaveFileMappings(List<string> FileNames, string zipFileName = null, CancellationToken ct = default)
        {
            _table.CreateIfNotExists(ct);
            zipFileName = zipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            Dictionary<string, List<TableTransactionAction>> batches = new Dictionary<string, List<TableTransactionAction>>();
            foreach (string fileName in FileNames)
            {
                string partitionKey = Utility.GetPartitionKey(fileName);

                if (!batches.ContainsKey(partitionKey))
                {
                    batches.Add(partitionKey, new List<TableTransactionAction> { });
                }

                batches[partitionKey].Add(new TableTransactionAction(
                    TableTransactionActionType.UpsertMerge,
                    new TableEntity(partitionKey, fileName) { { "ZipFileName", zipFileName } }));

                if (batches[partitionKey].Count == 100)
                {
                    _table.SubmitTransaction(batches[partitionKey]);
                    batches[partitionKey].Clear();
                }
            }
            foreach (string partKey in batches.Keys)
            {
                 _table.SubmitTransaction(batches[partKey]);
                batches[partKey].Clear();
            }
        }
        public async Task SaveFileMappingsAsync(List<string> FileNames, string zipFileName = null, CancellationToken ct = default)
        {
            await _table.CreateIfNotExistsAsync(ct);
            zipFileName = zipFileName ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.zip";
            Dictionary<string, List<TableTransactionAction>> batches = new Dictionary<string, List<TableTransactionAction>>();
            foreach (string fileName in FileNames)
            {
                string partitionKey = Utility.GetPartitionKey(fileName);

                if (!batches.ContainsKey(partitionKey))
                {
                    batches.Add(partitionKey, new List<TableTransactionAction> { });
                }

                batches[partitionKey].Add(new TableTransactionAction(
                    TableTransactionActionType.UpsertMerge,
                    new TableEntity(partitionKey, fileName) { { "ZipFileName", zipFileName } }));

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
