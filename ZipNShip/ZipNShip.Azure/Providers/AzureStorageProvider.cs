using System;
using Azure.Storage.Blobs;
using ZipNShip.Core;

namespace ZipNShip.Azure
{
    public partial class AzureStorageProvider : IStorageProvider
    {
        private readonly BlobContainerClient _container;
        private readonly IIndexStore _indexStore;
        public AzureStorageProvider(string ConnectionString, string ContainerName, string AzureTableName): this(ConnectionString, ContainerName, null, AzureTableName) { }
        public AzureStorageProvider(string ConnectionString, string ContainerName, IIndexStore IndexStore): this(ConnectionString, ContainerName, IndexStore, null) { }
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
                    throw new ArgumentException("Table name must be provided when using default Azure Table");

                _indexStore = new AzureIndexStore(connectionString, tableName);
            }
        }     
    }
}
