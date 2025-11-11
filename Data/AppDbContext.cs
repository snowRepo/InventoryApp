using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using InventoryApp.Models;

namespace InventoryApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "inventory.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Sku).HasMaxLength(64).IsRequired();
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            e.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).HasMaxLength(128).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.TransactionId).HasMaxLength(64).IsRequired();
            e.Property(s => s.ProductName).HasMaxLength(256).IsRequired();
            e.Property(s => s.Price).HasColumnType("decimal(18,2)");
            e.Property(s => s.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasIndex(s => s.TransactionId);
            e.HasIndex(s => s.SaleDate);
        });
    }
}
