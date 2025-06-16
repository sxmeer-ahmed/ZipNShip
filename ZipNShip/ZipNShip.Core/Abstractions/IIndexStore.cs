namespace ZipNShip.Core;
public interface IIndexStore
{
    Task SaveFileMappingsAsync(ZipNShipFile zipNShipFile, string zipFileName, CancellationToken ct = default);
}
