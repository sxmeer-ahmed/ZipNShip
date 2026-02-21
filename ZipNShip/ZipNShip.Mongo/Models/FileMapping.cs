using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZipNShip.Mongo.Models
{
    public class FileMapping
    {
        [BsonId]
        public Guid Id { get; set; }

        public string ZipName   { get; set; }
        public string FileName  { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
