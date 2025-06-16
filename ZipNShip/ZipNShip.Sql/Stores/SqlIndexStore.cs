using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZipNShip.Core.Abstractions;
using ZipNShip.Sql.Data;

namespace ZipNShip.Sql;
public class SqlIndexStore : IIndexStore
{
    private readonly MyDbContext _db;

    public SqlIndexStore(MyDbContext db) => _db = db;

    public async Task SaveFileMappingsAsync(string zipName, IEnumerable<string> fileNames, CancellationToken ct = default)
    {
        var entities = fileNames.Select(f => new FileMapping
        {
            Id        = Guid.NewGuid(),
            ZipName   = zipName,
            FileName  = f,
            CreatedAt = DateTime.UtcNow
        });

        _db.FileMappings.AddRange(entities);
        await _db.SaveChangesAsync(ct);
    }
}
