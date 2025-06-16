namespace ZipNShip.Core;
public interface IStorageProvider
{
    Task UploadAsync(ZipNShipFile zipNShipFile, string blobName, CancellationToken ct = default);
}
