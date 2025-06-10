using System;
using System.IO;
using System.IO.Compression;

namespace ZipNShip.Core.Models
{
    public class ZipNShipFile : IDisposable
    {
        public MemoryStream MemoryStream { get; }
        public ZipArchive ZipArchive    { get; }

        public ZipNShipFile()
        {
            MemoryStream = new MemoryStream();
            ZipArchive   = new ZipArchive(MemoryStream, ZipArchiveMode.Create, leaveOpen: true);
        }

        public void Dispose()
        {
            ZipArchive?.Dispose();
            MemoryStream?.Dispose();
        }
    }
}
