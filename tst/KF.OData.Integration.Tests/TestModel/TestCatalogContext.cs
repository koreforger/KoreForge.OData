using Microsoft.EntityFrameworkCore;

namespace KF.OData.Integration.Tests.TestModel;

public class TestCatalogContext : DbContext
{
    public TestCatalogContext(DbContextOptions<TestCatalogContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.ProductId);
            e.Property(p => p.ProductId).ValueGeneratedNever();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(o => o.OrderId);
            e.Property(o => o.OrderId).ValueGeneratedNever();
            e.Property(o => o.CreatedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(a => a.LogId);
            e.Property(a => a.LogId).ValueGeneratedNever();
        });
    }
}
