using AutoService.Consumer.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Consumer.Persistence;

public class ReadModelDbContext : DbContext
{
    public ReadModelDbContext(DbContextOptions<ReadModelDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceOrderReadModel> ServiceOrders => Set<ServiceOrderReadModel>();
    public DbSet<ServiceOrderWorkReadModel> ServiceOrderWorks => Set<ServiceOrderWorkReadModel>();
    public DbSet<ServiceOrderPartReadModel> ServiceOrderParts => Set<ServiceOrderPartReadModel>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceOrderReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ServiceOrderWorkReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ServiceOrderPartReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ProcessedEvent>().HasKey(x => x.EventId);

        modelBuilder.Entity<ProcessedEvent>().HasIndex(x => x.EventId).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
