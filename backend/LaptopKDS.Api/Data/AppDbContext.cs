using LaptopKDS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopKDS.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Laptop> Laptops => Set<Laptop>();
    public DbSet<DecisionProfile> DecisionProfiles => Set<DecisionProfile>();
    public DbSet<ProfileCriterionWeight> ProfileCriterionWeights => Set<ProfileCriterionWeight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Laptop ---
        modelBuilder.Entity<Laptop>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Brand).IsRequired().HasMaxLength(100);
            e.Property(l => l.Model).IsRequired().HasMaxLength(200);
            e.Property(l => l.Price).HasColumnType("decimal(10,2)");
            e.Property(l => l.SourceUrl).HasMaxLength(500);
        });

        // --- DecisionProfile ---
        modelBuilder.Entity<DecisionProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Description).HasMaxLength(500);
            e.Property(p => p.IconEmoji).HasMaxLength(10);
        });

        // --- ProfileCriterionWeight ---
        modelBuilder.Entity<ProfileCriterionWeight>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasOne(w => w.Profile)
             .WithMany(p => p.CriterionWeights)
             .HasForeignKey(w => w.ProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
