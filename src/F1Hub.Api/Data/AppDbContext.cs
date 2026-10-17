using F1Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1Hub.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasMany(x => x.Drivers)
             .WithOne(d => d.Team)
             .HasForeignKey(d => d.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Products)
             .WithOne(p => p.Team)
             .HasForeignKey(p => p.TeamId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Driver>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.FavoriteTeam)
             .WithMany()
             .HasForeignKey(x => x.FavoriteTeamId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FavoriteDriver)
             .WithMany()
             .HasForeignKey(x => x.FavoriteDriverId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Author)
             .WithMany(u => u.Articles)
             .HasForeignKey(x => x.AuthorId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Team)
             .WithMany()
             .HasForeignKey(x => x.TeamId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Driver)
             .WithMany()
             .HasForeignKey(x => x.DriverId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(x => x.Status).IsRequired().HasMaxLength(30);
            e.Property(x => x.TotalAmount).HasPrecision(10, 2);
            e.HasOne(x => x.User)
             .WithMany(u => u.Orders)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
            e.HasOne(x => x.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
