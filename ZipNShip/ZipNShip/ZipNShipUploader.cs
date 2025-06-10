using Azure.Storage.Blobs;
using CSharpVitamins;
using ZipnShip.Azure;

namespace ZipNShip.Core;
public class ZipNShipUploader : IDisposable
{
    private readonly IAzureHelper AzureHelper;

    public ZipNShipUploader(IAzureHelper azureHelper)
    {
        AzureHelper = azureHelper;
    }
    public void UploadZipToAzureBlob(ZipNShipFile zipNShipFile)
    {
        _blobContainerClient.CreateIfNotExistsAsync();
        string blobName = $"{ShortGuid.NewGuid()}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        blobClient.UploadAsync(zipNShipFile._zipStream, overwrite: true);
        Console.WriteLine($"Uploaded ZIP: {blobName}");

    }
    
}
