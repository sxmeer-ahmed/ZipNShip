using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;

namespace ZipNShip.Core
{
    public class ZipNShipFile : IDisposable
    {
        private ZipArchive _zipArchive;
        private MemoryStream zipStream { get; set; }
        public List<string> fileNames {  get; set; } = new List<string>();
        private readonly IStorageProvider _storageProvider;

        private readonly long _maxSizeInBytes;
        private readonly bool _autoSplit;
        private readonly bool _allowDuplicacy;

        private long _currentSizeInBytes;
        private bool _isFinalized = false;
        public event EventHandler SizeLimitReached;

        public bool IsSizeLimitReached => _currentSizeInBytes >= _maxSizeInBytes;
        public long CurrentSizeInBytes => _currentSizeInBytes;
        public long RemainingSizeInBytes => _maxSizeInBytes - _currentSizeInBytes;

        public ZipNShipFile(ZipNShipOptions options)
        {
            _maxSizeInBytes = options.MaxSizeInKB * 1024;
            _autoSplit = options.AutoSplit;
            _storageProvider = options.StorageProvider;
            zipStream = new MemoryStream();
            _zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
        }
        public string PushObject<T>(T obj, string customName = null, bool useMessagePack = false)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            string className = typeof(T).Name;
            string guid = Guid.NewGuid().ToString("N");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            string baseName = string.IsNullOrWhiteSpace(customName) ? className : customName;
            string extension = useMessagePack ? ".mpack" : ".json";
            string fileName = $"{baseName}_{guid}_{timestamp}{extension}";

            fileNames.Add(fileName);

            var entry = _zipArchive.CreateEntry(fileName);

            using (var entryStream = entry.Open())
            {
                if (useMessagePack)
                {
                    byte[] data = MessagePackSerializer.Serialize(obj);

                    var fileSize = data.Length;

                    if (_currentSizeInBytes + fileSize > _maxSizeInBytes)
                    {
                        SizeLimitReached?.Invoke(this, EventArgs.Empty);

                        if (_autoSplit)
                        {
                            _storageProvider.UploadAsync(zipStream, fileNames);
                        }
                        else
                        {
                            throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                        }
                    }

                    entryStream.Write(data, 0, data.Length);
                    _currentSizeInBytes += fileSize;
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    using (var writer = new StreamWriter(entryStream))
                    {
                        string json = JsonSerializer.Serialize(obj, options);
                        var fileSize = json.Length;

                        if (_currentSizeInBytes + fileSize > _maxSizeInBytes)
                        {
                            SizeLimitReached?.Invoke(this, EventArgs.Empty);

                            if (_autoSplit)
                            {
                                _storageProvider.UploadAsync(zipStream, fileNames);
                            }
                            else
                            {
                                throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                            }
                        }
                        writer.Write(json);
                        _currentSizeInBytes += fileSize;
                    }
                }
            }

            return fileName;
        }
        public bool PushFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            foreach (string file in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = file.Substring(folderPath.Length).TrimStart(Path.DirectorySeparatorChar);
                var entry = _zipArchive.CreateEntry(relativePath);

                using (var fileStream = File.OpenRead(file))
                using (var entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }
            }
            return true;
        }
        // 
        // Summary:
        //       Push File Data into Stream
        //
        // Parameters:
        //      filePath:
        //          Share File Path Where File is Stored
        public string PushFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string fileName = $"{Path.GetFileName(filePath)}_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            fileNames.Add(fileName);

            using (var fileStream = File.OpenRead(filePath))
            {
                var fileSize = fileStream.Length;

                if (_currentSizeInBytes + fileSize > _maxSizeInBytes)
                {
                    SizeLimitReached?.Invoke(this, EventArgs.Empty);

                    if (_autoSplit)
                    {
                        FinalizeZip();
                        _storageProvider.UploadAsync(zipStream, fileNames);
                        ResetZip();
                    }
                    else
                    {
                        throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                    }
                }

                ZipArchiveEntry entry = _zipArchive.CreateEntry(fileName);

                using (Stream entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }

                _currentSizeInBytes += fileSize;
            }
            return fileName;
        }
        public void FinalUpload()
        {
            FinalizeZip();
            _storageProvider.UploadAsync(zipStream, fileNames);
            ResetZip();
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
