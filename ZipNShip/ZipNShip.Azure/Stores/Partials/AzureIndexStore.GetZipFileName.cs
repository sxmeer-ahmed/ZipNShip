using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public partial class AzureIndexStore
    {
		public string GetZipFileName(string fileName, CancellationToken ct = default)
		{
			_table.CreateIfNotExists(ct);

			try
			{
				string partKey = Utility.GetPartitionKey(fileName);
				Response<TableEntity> resp =
					_table.GetEntity<TableEntity>(
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

		public async Task<string> GetZipFileNameAsync(string fileName, CancellationToken ct = default)
		{
			await _table.CreateIfNotExistsAsync(ct);

			try
			{
				string partKey = Utility.GetPartitionKey(fileName);
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
	}
}
