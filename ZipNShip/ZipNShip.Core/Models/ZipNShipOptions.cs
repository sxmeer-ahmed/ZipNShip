namespace ZipNShip.Core
{
    public class ZipNShipOptions
    {
        public long MaxSizeInMB { get; set; } = 200;
        public bool AutoSplit { get; set; } = false;
        public bool AllowDuplicacy { get; set; } = false;

        /// <summary>
        /// Optional. If set, ZipNShipFile can auto-upload and reset based on MaxSize.
        /// </summary>
        public IStorageProvider StorageProvider { get; set; }
    }
}