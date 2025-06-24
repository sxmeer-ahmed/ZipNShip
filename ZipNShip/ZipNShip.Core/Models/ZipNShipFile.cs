using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;

namespace ZipNShip.Core
{
    public class ZipNShipFile : IDisposable
    {
        private readonly ZipArchive _zipArchive;
        public MemoryStream zipStream;
        public Dictionary<string, long> fileNames = new Dictionary<string, long>();
        private readonly IStorageProvider _storageProvider;

        private readonly long _maxSizeInBytes;
        private readonly bool _autoSplit;
        private readonly bool _allowDuplicacy;

        private long _currentSizeInBytes;
        public event EventHandler SizeLimitReached;

        public bool IsSizeLimitReached => _currentSizeInBytes >= _maxSizeInBytes;
        public long CurrentSizeInBytes => _currentSizeInBytes;
        public long RemainingSizeInBytes => _maxSizeInBytes - _currentSizeInBytes;

        public ZipNShipFile(ZipNShipOptions options)
        {
            _maxSizeInBytes = options.MaxSizeInMB * 1024 * 1024;
            _autoSplit = options.AutoSplit;
            _allowDuplicacy = options.AllowDuplicacy;
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

            var entry = _zipArchive.CreateEntry(fileName);

            // Replace 'using var' with explicit 'using' block for compatibility with C# 7.3
            using (var entryStream = entry.Open())
            {
                if (useMessagePack)
                {
                    byte[] data = MessagePackSerializer.Serialize(obj);
                    entryStream.Write(data, 0, data.Length);
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
                        writer.Write(json);
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
                // Replace the problematic line with the following code to manually calculate the relative path:
                var relativePath = file.Substring(folderPath.Length).TrimStart(Path.DirectorySeparatorChar);
                var entry = _zipArchive.CreateEntry(relativePath);

                // Replace 'using var' with explicit 'using' block for compatibility with C# 7.3
                using (var fileStream = File.OpenRead(file))
                using (var entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }
            }
            return true;
        }
        public string PushFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var fileName = Path.GetFileName(filePath);
            if (fileNames.ContainsKey(fileName) && !_allowDuplicacy)
                throw new Exception("File with this name exisits already allow rename");

            using (var fileStream = File.OpenRead(filePath))
            {
                var fileSize = fileStream.Length;

                if (_currentSizeInBytes + fileSize > _maxSizeInBytes)
                {
                    SizeLimitReached?.Invoke(this, EventArgs.Empty);

                    if (_autoSplit)
                    {
                        _storageProvider.UploadAsync(zipStream, fileNames.Keys.ToList());
                    }
                    else
                    {
                        throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                    }
                }

                fileName += fileNames[fileName] == 0 ? "" : fileNames[fileName].ToString();
                ZipArchiveEntry entry = _zipArchive.CreateEntry(fileName);

                // Replace 'using var' with explicit 'using' block for compatibility with C# 7.3
                using (Stream entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }

                _currentSizeInBytes += fileSize;
                ++fileNames[fileName];
            }
            return fileName;
        }

        public byte[] GetZipBytes()
        {
            _zipArchive.Dispose();
            return zipStream.ToArray();
        }

        public void Dispose()
        {
            _zipArchive.Dispose();
            zipStream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
