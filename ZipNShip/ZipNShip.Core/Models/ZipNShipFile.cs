using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ZipNShip.Core;
public class ZipNShipFile : IDisposable
{
    private readonly ZipArchive _zipArchive;
    public MemoryStream zipStream;
    private readonly long _maxSizeInBytes;
    private readonly bool _autoSplit;
    private readonly bool _allowDuplicacy;
    private long _currentSizeInBytes;
    private int _partCounter = 1;
    public Dictionary<string, long> fileNames = [];

    public event EventHandler? SizeLimitReached;

    public bool IsSizeLimitReached => _currentSizeInBytes >= _maxSizeInBytes;
    public long CurrentSizeInBytes => _currentSizeInBytes;
    public long RemainingSizeInBytes => _maxSizeInBytes - _currentSizeInBytes;

    public ZipNShipFile(long maxSizeInMB = 200, bool autoSplit = false, bool allowDuplicacy = false)
    {
        _maxSizeInBytes = maxSizeInMB * 1024 * 1024;
        _autoSplit = autoSplit;
        _allowDuplicacy = allowDuplicacy;
        zipStream = new MemoryStream();
        _zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
    }
    public void PushFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        foreach (string file in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
        {
            var relativePath = Path.GetRelativePath(folderPath, file);
            var entry = _zipArchive.CreateEntry(relativePath);
            using var fileStream = File.OpenRead(file);
            using var entryStream = entry.Open();
            fileStream.CopyTo(entryStream);
        }
    }
    public string PushFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileName = Path.GetFileName(filePath);
        if (fileNames.Keys.Contains(fileName) && !_allowDuplicacy)
            throw new Exception("File with this name exisits already allow rename");



        using var fileStream = File.OpenRead(filePath);
        var fileSize = fileStream.Length;

        if (_currentSizeInBytes + fileSize > _maxSizeInBytes)
        {
            SizeLimitReached?.Invoke(this, EventArgs.Empty);

            if (_autoSplit)
            {
                FlushZipAndStartNew();
            }
            else
            {
                throw new Exception("File size is too big, Increase Memory or Allow AutoSplt for Current Files");
            }
        }

        fileName += fileNames[fileName] == 0 ? "" : fileNames[fileName].ToString();
        ZipArchiveEntry entry = _zipArchive.CreateEntry(fileName);
        using Stream? entryStream = entry.Open();
        fileStream.CopyTo(entryStream);

        _currentSizeInBytes += fileSize;
        ++fileNames[fileName];
        return fileName;
    }

    private void FlushZipAndStartNew()
    {
        // Here user should upload _zipStream if needed (provide method for that)
        _zipStream.Position = 0;
        var bytes = _zipStream.ToArray();
        File.WriteAllBytes($"part{_partCounter++}.zip", bytes); // Replace with blob upload

        // Reset
        _zipStream.SetLength(0);
        _zipStream.Position = 0;
        _zipArchive.Dispose();

        _zipArchive = new ZipArchive(_zipStream, ZipArchiveMode.Create, true);
        _currentSizeInBytes = 0;
        _addedFiles.Clear();
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace("\\", "/");
    }

    public byte[] GetZipBytes()
    {
        _zipArchive.Dispose();
        return _zipStream.ToArray();
    }

    public void Dispose()
    {
        _zipArchive.Dispose();
        zipStream.Dispose();
    }

    public void PushZip(string zipPath)
    {
        zipPath = NormalizePath(zipPath);
        if (!File.Exists(zipPath)) throw new FileNotFoundException($"Zip not found: {zipPath}");

        using var sourceZip = ZipFile.OpenRead(zipPath);
        foreach (var entry in sourceZip.Entries)
        {
            var tempStream = new MemoryStream();
            using var entryStream = entry.Open();
            entryStream.CopyTo(tempStream);
            tempStream.Position = 0;

            var targetEntry = _zipArchive.CreateEntry(entry.FullName);
            using var targetStream = targetEntry.Open();
            tempStream.CopyTo(targetStream);
        }
    }
    public Stream FinalizeZip()
    {
        _zipArchive.Dispose();
        if (_inMemory)
        {
            _zipStream.Position = 0;
            return _zipStream;
        }
        else
        {
            using var file = File.Create(_outputPath);
            _zipStream.Position = 0;
            _zipStream.CopyTo(file);
            return null;
        }
    }

    public void Dispose()
    {
        _zipArchive?.Dispose();
        zipStream?.Dispose();
    }

    private string NormalizePath(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return path.Replace('/', '\\');
        else
            return path.Replace('\\', '/');
    }
}
