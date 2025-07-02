using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;

namespace ZipNShip.Core
{
    public partial class ZipNShipFile
    {
        public async Task<string> PushObjectAsync<T>(T obj, bool useMessagePack)
        {
            return await PushObjectHelperAsync<T>(obj, null, useMessagePack);
        }
        public string PushObject<T>(T obj, bool useMessagePack)
        {
            return PushObjectHelper<T>(obj, null, useMessagePack);
        }
        public async Task<string> PushObjectAsync<T>(T obj, string customName = null, bool useMessagePack = false)
        {
            return await PushObjectHelperAsync<T>(obj, customName, useMessagePack);
        }
        public string PushObject<T>(T obj, string customName = null, bool useMessagePack = false)
        {
            return PushObjectHelper<T>(obj, customName, useMessagePack);
        }
        private async Task<string> PushObjectHelperAsync<T>(T obj, string customName, bool useMessagePack)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            string className = typeof(T).Name;
            string guid = Guid.NewGuid().ToString("N");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            string baseName = string.IsNullOrWhiteSpace(customName) ? className : customName;
            string extension = useMessagePack ? ".mpack" : ".json";
            string fileName = $"{guid}_{timestamp}_{baseName}.{extension}";

            fileNames.Add(fileName);

            var entry = _zipArchive.CreateEntry(fileName);

            using (var entryStream = entry.Open())
            {
                if (useMessagePack)
                    await MessagePackHelperAsync(obj, entryStream);
                else
                    await JsonHelperAsync(obj, entryStream);
            }

            return fileName;
        }

        private string PushObjectHelper<T>(T obj, string customName, bool useMessagePack)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            string className = typeof(T).Name;
            string guid = Guid.NewGuid().ToString("N");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            string baseName = string.IsNullOrWhiteSpace(customName) ? className : customName;
            string extension = useMessagePack ? ".mpack" : ".json";
            string fileName = $"{guid}_{timestamp}_{baseName}.{extension}";

            fileNames.Add(fileName);

            var entry = _zipArchive.CreateEntry(fileName);

            using (var entryStream = entry.Open())
            {
                if (useMessagePack)
                    MessagePackHelper(obj, entryStream);
                else
                    JsonHelper(obj, entryStream);
            }

            return fileName;
        }

        private async Task JsonHelperAsync<T>(T obj, Stream entryStream)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(obj, settings);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            ulong fileSize = (ulong)jsonBytes.Length;

            if (currentSizeInBytes + fileSize > _maxSizeInBytes)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);

                if (_autoSplitStorageProvider != null)
                {
                    await _autoSplitStorageProvider.UploadAsync(zipStream, fileNames);
                }
                else
                {
                    throw new Exception("File size is too big. Increase memory limit or allow AutoSplit for current files.");
                }
            }

            await entryStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            currentSizeInBytes += fileSize;
        }
        private void JsonHelper<T>(T obj, Stream entryStream)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(obj, settings);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            ulong fileSize = (ulong)jsonBytes.Length;

            if (currentSizeInBytes + fileSize > _maxSizeInBytes)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);

                if (_autoSplitStorageProvider != null)
                {
                    _autoSplitStorageProvider.Upload(zipStream, fileNames);
                }
                else
                {
                    throw new Exception("File size is too big. Increase memory limit or allow AutoSplit for current files.");
                }
            }

            entryStream.Write(jsonBytes, 0, jsonBytes.Length);
            currentSizeInBytes += fileSize;
        }


        private async Task MessagePackHelperAsync<T>(T obj, Stream entryStream)
        {
            byte[] data = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolver.Options);

            ulong fileSize = (ulong)data.Length;

            if (currentSizeInBytes + fileSize > _maxSizeInBytes)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);

                if (_autoSplitStorageProvider != null)
                {
                    await _autoSplitStorageProvider.UploadAsync(zipStream, fileNames);
                }
                else
                {
                    throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                }
            }

            await entryStream.WriteAsync(data, 0, data.Length);
            currentSizeInBytes += fileSize;
        }

        private void MessagePackHelper<T>(T obj, Stream entryStream)
        {
            byte[] data = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolver.Options);

            ulong fileSize = (ulong)data.Length;

            if (currentSizeInBytes + fileSize > _maxSizeInBytes)
            {
                SizeLimitReached?.Invoke(this, EventArgs.Empty);

                if (_autoSplitStorageProvider != null)
                {
                    _autoSplitStorageProvider.Upload(zipStream, fileNames);
                }
                else
                {
                    throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
                }
            }

            entryStream.Write(data, 0, data.Length);
            currentSizeInBytes += fileSize;
        }
    }
}
