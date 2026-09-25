using AutoService.ReadModel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoService.ReadModel.Api.Persistence;

public class ReadModelDbContext : DbContext
{
    public ReadModelDbContext(DbContextOptions<ReadModelDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceOrderReadModel> ServiceOrders => Set<ServiceOrderReadModel>();
    public DbSet<ServiceOrderWorkReadModel> ServiceOrderWorks => Set<ServiceOrderWorkReadModel>();
    public DbSet<ServiceOrderPartReadModel> ServiceOrderParts => Set<ServiceOrderPartReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceOrderReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ServiceOrderWorkReadModel>().HasKey(x => x.Id);
        modelBuilder.Entity<ServiceOrderPartReadModel>().HasKey(x => x.Id);
        base.OnModelCreating(modelBuilder);
    }
}
