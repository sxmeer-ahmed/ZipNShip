using Microsoft.EntityFrameworkCore;
using ZipNShip.Sql.Data;

namespace ZipNShip.Sql.Data
{
    public class MyDbContext : DbContext
    {
        public DbSet<FileMapping> FileMappings { get; set; }

        public MyDbContext(DbContextOptions<MyDbContext> opts)
            : base(opts)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileMapping>()
                .HasKey(f => f.Id);
        }
    }
}
