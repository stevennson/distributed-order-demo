using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    public DbSet<Product> Products=>Set<Product>();
    public DbSet<Order> Orders=>Set<Order>();
    public DbSet<ProcessedMessage> ProcessedMessages=>Set<ProcessedMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>().HasIndex(x=>x.EventId).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}

