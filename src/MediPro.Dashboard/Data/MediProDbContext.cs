using MediPro.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MediPro.Dashboard.Data;

public class MediProDbContext : DbContext
{
    public MediProDbContext(DbContextOptions<MediProDbContext> options) : base(options)
    {
    }

    public DbSet<PndtLogEntry> PndtLogs => Set<PndtLogEntry>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PndtLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => e.PatientId);
        });
    }
}
