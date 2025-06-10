using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClickHouse.Ado;
using ZipNShip.Core.Abstractions;

namespace ZipNShip.ClickHouse.Stores
{
    public class ClickHouseIndexStore : IIndexStore
    {
        private readonly ClickHouseConnection _conn;

        public ClickHouseIndexStore(string connectionString)
        {
            _conn = new ClickHouseConnection(connectionString);
        }

        public async Task SaveFileMappingsAsync(string zipName, IEnumerable<string> fileNames, CancellationToken ct = default)
        {
            await _conn.OpenAsync(ct);
            await _conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS zip_file_index (
                  ZipName   String,
                  FileName  String,
                  CreatedAt DateTime
                ) ENGINE = MergeTree() ORDER BY tuple()
            ", ct);

            using var cmd = _conn.CreateCommand(@"
                INSERT INTO zip_file_index (ZipName, FileName, CreatedAt) VALUES @bulk
            ");
            cmd.Parameters.AddWithValue("bulk", fileNames
                .Select(f => new object[] { zipName, f, DateTime.UtcNow })
                .ToArray());
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
