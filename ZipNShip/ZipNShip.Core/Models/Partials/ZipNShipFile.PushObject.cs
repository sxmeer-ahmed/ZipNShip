using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MessagePack;

namespace ZipNShip.Core
{
    public partial class ZipNShipFile
    {
        public async Task<string> PushObjectAsync<T>(T obj, bool useMessagePack = false)
        {
            return await PushObjectHelper<T>(obj, null, useMessagePack, isAsync: false);
        }
        public string PushObject<T>(T obj, bool useMessagePack = false)
        {
            return PushObjectHelper<T>(obj, null, useMessagePack, isAsync: false).GetAwaiter().GetResult();
        }
        public async Task<string> PushObjectAsync<T>(T obj, string customName = null, bool useMessagePack = false)
        {
            return await PushObjectHelper<T>(obj, customName, useMessagePack, isAsync: false);
        }
        public string PushObject<T>(T obj, string customName = null, bool useMessagePack = false)
        {
            return PushObjectHelper<T>(obj, customName, useMessagePack, isAsync: false).GetAwaiter().GetResult();
        }
        private async Task<string> PushObjectHelper<T>(T obj, string customName, bool useMessagePack, bool isAsync)
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
                    if(isAsync)
                        await MessagePackHelperAsync(obj, entryStream);
                    else
                        MessagePackHelperAsync(obj, entryStream).GetAwaiter().GetResult();
                }
                else
                {
                    if(isAsync)
                        await JsonHelperAsync(obj, entryStream);
                    else
                       JsonHelperAsync(obj, entryStream).GetAwaiter().GetResult();
                }
            }

            return fileName;
        }

        private async Task JsonHelperAsync<T>(T obj, Stream entryStream)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using (var writer = new StreamWriter(entryStream))
            {
                string json = JsonSerializer.Serialize(obj, options);
                ulong fileSize = (ulong)json.Length;

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
                await writer.WriteAsync(json);
                currentSizeInBytes += fileSize;
            }
        }

        private async Task MessagePackHelperAsync<T>(T obj, Stream entryStream)
        {
            byte[] data = MessagePackSerializer.Serialize(obj);

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
    }
}
