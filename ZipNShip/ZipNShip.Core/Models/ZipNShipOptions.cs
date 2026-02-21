namespace ZipNShip.Core
{
    public class ZipNShipOptions
    {
        public ulong MaxSizeInMB { get; set; } = 200;
        public IStorageProvider AutoSplitStorageProvider { get; set; }
    }
}