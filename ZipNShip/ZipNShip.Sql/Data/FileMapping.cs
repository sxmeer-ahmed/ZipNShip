using System;

namespace ZipNShip.Sql.Data
{
    public class FileMapping
    {
        public Guid   Id        { get; set; }
        public string ZipName   { get; set; }
        public string FileName  { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
