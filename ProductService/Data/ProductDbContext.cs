using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed initial products with correct, unique IDs
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop",   Price = 999.90m },
            new Product { Id = 2, Name = "Mouse",    Price = 24.90m  },
            new Product { Id = 3, Name = "Keyboard", Price = 49.90m  }
        );
    }
}
