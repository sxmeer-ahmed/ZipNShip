using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ZipNShip.Core.Abstractions;
using ZipNShip.Mongo.Models;

namespace ZipNShip.Mongo.Stores
{
    public class MongoIndexStore : IIndexStore
    {
        private readonly IMongoCollection<FileMapping> _col;

        public MongoIndexStore(string connectionString, string database)
        {
            var client = new MongoClient(connectionString);
            _col = client.GetDatabase(database)
                         .GetCollection<FileMapping>("zip_file_index");
        }

        public Task SaveFileMappingsAsync(string zipName, IEnumerable<string> fileNames, CancellationToken ct = default)
        {
            var docs = fileNames.Select(f => new FileMapping
            {
                Id        = Guid.NewGuid(),
                ZipName   = zipName,
                FileName  = f,
                CreatedAt = DateTime.UtcNow
            });
            return _col.InsertManyAsync(docs, cancellationToken: ct);
        }
    }
}
