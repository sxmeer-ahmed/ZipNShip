using Azure.Data.Tables;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public partial class AzureIndexStore : IIndexStore
    {
        private readonly TableClient _table;

        public AzureIndexStore(string connectionString, string tableName)
        {
            _table = new TableClient(connectionString, tableName);
        }
    }
}
