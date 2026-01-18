using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            entity.Property(e => e.Balance)
                .HasPrecision(18, 2) // For currency: 18 total digits, 2 decimal places
                .IsRequired();
        });
    }
}
