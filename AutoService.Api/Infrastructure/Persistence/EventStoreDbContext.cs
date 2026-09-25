using AutoService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Api.Infrastructure.Persistence;

public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options)
    {
    }

    public DbSet<EventStoreEntity> Events => Set<EventStoreEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStoreEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AggregateId, e.SequenceNumber }).IsUnique();
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }
}
