using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ZipNShip.Core
{
    public partial class ZipNShipFile : IDisposable
    {
        private ZipArchive _zipArchive;
        private MemoryStream zipStream { get; set; }
        public List<string> fileNames {  get; set; } = new List<string>();
        private readonly IStorageProvider _autoSplitStorageProvider = null;

        private readonly ulong _maxSizeInBytes;
        public ulong currentSizeInBytes;
        private bool _isFinalized = false;
        public event EventHandler SizeLimitReached;

        public bool IsSizeLimitReached => currentSizeInBytes >= _maxSizeInBytes;
        public ulong RemainingSizeInBytes => _maxSizeInBytes - currentSizeInBytes;

        public ZipNShipFile(ZipNShipOptions options)
        {
            _maxSizeInBytes = options.MaxSizeInMB * 1024 * 1024;
            _autoSplitStorageProvider = options.AutoSplitStorageProvider;
            zipStream = new MemoryStream();
            _zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
        }
        public async Task FinalUploadAsync()
        {
            if (_autoSplitStorageProvider != null)
            {
                FinalizeZip();
                await _autoSplitStorageProvider.UploadAsync(zipStream, fileNames);
                ResetZip();
            }
            else
                throw new InvalidOperationException("Please Pass Auto Split Storage Provider in ZipNShipOptions While Declaring ZipNShip to Access this Function");
        }
        public void FinalUpload()
        {
            if (_autoSplitStorageProvider != null)
            {
                FinalizeZip();
                _autoSplitStorageProvider.Upload(zipStream, fileNames);
                ResetZip();
            }
            else
                throw new InvalidOperationException("Please Pass Auto Split Storage Provider in ZipNShipOptions While Declaring ZipNShip to Access this Function");
        }
        public void FinalizeZip()
        {
            if (!_isFinalized)
            {
                _zipArchive.Dispose(); 
                zipStream.Position = 0; 
                _isFinalized = true;
            }
        }

        public MemoryStream GetZipStream()
        {
            if (!_isFinalized)
                throw new InvalidOperationException("FinalizeZip must be called before accessing the ZIP stream.");

            return zipStream;
        }
        public void ResetZip()
        {
            _zipArchive?.Dispose();
            zipStream.SetLength(0);
            zipStream.Position = 0;
            _zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);
            _isFinalized = false;
            fileNames.Clear();
        }

        public void Dispose()
        {
            _zipArchive?.Dispose();
            zipStream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
