using Microsoft.EntityFrameworkCore;
using CryptoAnalysis.App.Domain;

namespace CryptoAnalysis.App.Infrastructure
{
    public class CryptoDbContext : DbContext
    {
        public DbSet<Asset> Assets { get; set; }
        public DbSet<MarketData> MarketDataPoints { get; set; }
        public DbSet<CorrelationMetrics> CorrelationMetrics { get; set; }
        public DbSet<VolatilityMetrics> VolatilityMetrics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=postgres");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketData>()
            .HasIndex(m => new { m.AssetId, m.Timestamp })
            .IsUnique();

            modelBuilder.Entity<MarketData>()
            .HasOne(m => m.Asset)
            .WithMany()
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Cascade);;

            modelBuilder.Entity<VolatilityMetrics>()
            .HasOne<Asset>()
            .WithMany()
            .HasForeignKey(v => v.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CorrelationMetrics>()
            .HasOne<Asset>()
            .WithMany()
            .HasForeignKey(c => c.AssetId1)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CorrelationMetrics>()
            .HasOne<Asset>()
            .WithMany()
            .HasForeignKey(c => c.AssetId2)
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}