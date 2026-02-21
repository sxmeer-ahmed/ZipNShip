using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;

namespace ZipNShip.Azure
{
    public partial class AzureStorageProvider
    {
        public async Task<T> ReturnObjectAsync<T>(string fileName, CancellationToken ct = default)
        {
            MemoryStream zipStream = await GetZipMemoryStreamAsync(fileName, ct);

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entry = archive.GetEntry(fileName)
                    ?? throw new FileNotFoundException(
                        $"The Mapped-Zip does not contain an entry named '{fileName}'.");

                using (var entryStream = entry.Open())
                {
                    if (Path.GetExtension(fileName).Equals(".mpack", StringComparison.OrdinalIgnoreCase))
                    {
                        return await MessagePackSerializer.DeserializeAsync<T>(entryStream, TypelessContractlessStandardResolver.Options, cancellationToken: ct);
                    }
                    else
                    {
                        return await JsonSerializer.DeserializeAsync<T>(entryStream, cancellationToken: ct);
                    }
                }
            }
        }
        public T ReturnObject<T>(string fileName, CancellationToken ct = default)
        {
            MemoryStream zipStream = GetZipMemoryStream(fileName, ct);

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entry = archive.GetEntry(fileName)
                    ?? throw new FileNotFoundException(
                        $"The Mapped-Zip does not contain an entry named '{fileName}'.");

                using (var entryStream = entry.Open())
                {
                    if (Path.GetExtension(fileName).Equals(".mpack", StringComparison.OrdinalIgnoreCase))
                    {
                        return MessagePackSerializer.Deserialize<T>(entryStream, TypelessContractlessStandardResolver.Options, cancellationToken: ct);
                    }
                    else
                    {
                        return JsonSerializer.Deserialize<T>(entryStream);
                    }
                }
            }
        }
    }
}
