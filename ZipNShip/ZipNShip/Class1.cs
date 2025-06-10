using Azure.Data.Tables;
using System.Text;
using System.IO.Compression;
const string tableName = "AdCustomizerEvidence";
const string containerName = "adcustomizer";
const string sourceFolderPath = @"C:\Users\s.a.mee10\Pictures\06022025_07"; // your folder path
const string zipFilePath = @"C:\Users\s.a.mee10\Pictures\06022025_07.zip"; // your zip file path
//const string zipFileName = Path.GetFileName(zipFilePath); // used in mapping
const int batchSize = 100;
// Create ZIP from folder
CreateZipOnTheFlyAsync(sourceFolderPath, zipFilePath);
// Optional: Upload ZIP to Blob Storage
UploadZipToBlob(zipFilePath, containerName).Wait();
// Read all text file names from ZIP
var guidFileNames = GetFileNamesFromZip(zipFilePath);
var tableClient = new TableClient(storageConnectionString, tableName);
await tableClient.CreateIfNotExistsAsync();
Console.WriteLine($"Indexing {guidFileNames.Count} files...");
var batch = new List<TableTransactionAction>();
foreach (var guid in guidFileNames)
{
    var partitionKey = guid.Substring(0, 1); // simple sharding
    var entity = new TableEntity(partitionKey, guid)
    {
        { "ZipFileName", "06022025_07" }
    };
    batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, entity));
    if (batch.Count == batchSize)
    {
        await tableClient.SubmitTransactionAsync(batch);
        batch.Clear();
    }
}
if (batch.Count > 0)
{
    await tableClient.SubmitTransactionAsync(batch);
}
Console.WriteLine("Indexing completed.");
// ------------ Helpers ------------
static async Task UploadZipToBlob(string filePath, string containerName)
{
    var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(;
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    await containerClient.CreateIfNotExistsAsync();
    var blobName = Path.GetFileName(filePath);
    var blobClient = containerClient.GetBlobClient(blobName);
    using var fileStream = File.OpenRead(filePath);
    await blobClient.UploadAsync(fileStream, overwrite: true);
    Console.WriteLine($"Uploaded ZIP: {blobName}");
}
static List<string> GetFileNamesFromZip(string zipPath)
{
    var names = new List<string>();
    using var archive = ZipFile.OpenRead(zipPath);
    foreach (var entry in archive.Entries)
        names.Add(Path.GetFileName(entry.FullName));
    return names;
}
static void CreateZipOnTheFlyAsync(string sourceFolderPath, string destinationZipPath)
{
    try
    {
        if (File.Exists(destinationZipPath))
        {
            File.Delete(destinationZipPath); // Overwrite if exists
        }
        using var zipToCreate = new FileStream(destinationZipPath, FileMode.Create);
        using var archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create);
        int fileCount = 0;
        foreach (var filePath in Directory.EnumerateFiles(sourceFolderPath, "*", SearchOption.TopDirectoryOnly))
        {
            var entryName = Path.GetFileName(filePath);
            var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(filePath);
            fileStream.CopyTo(entryStream);
            Console.WriteLine($"Added: {entryName}");
            fileCount++;
        }
        Console.WriteLine($"ZIP created at: {destinationZipPath} with {fileCount} files.");
    }
    catch (Exception ex)
    {
    }
}