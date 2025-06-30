namespace ZipNShip.Core
{
    public class ZipNShipOptions
    {
        //
        //      AzureTableName:
        //          Azure Table Name to Store File-to-Zip Name Mapping
        public long MaxSizeInKB { get; set; } = 200;
        //
        //      AzureTableName:
        //          Azure Table Name to Store File-to-Zip Name Mapping
        public bool AutoSplit { get; set; } = false;
        //
        //      AzureTableName:
        //          Azure Table Name to Store File-to-Zip Name Mapping
        public IStorageProvider StorageProvider { get; set; }
    }
}