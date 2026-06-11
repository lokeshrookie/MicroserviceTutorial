using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed two sample orders (fixed dates to keep seed data stable)
        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id        = 1,
                ProductId = 1,
                Quantity  = 2,
                OrderDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Status    = "Placed"
            },
            new Order
            {
                Id        = 2,
                ProductId = 2,
                Quantity  = 3,
                OrderDate = new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc),
                Status    = "Placed"
            }
        );
    }
}
