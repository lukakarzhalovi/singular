using Microsoft.EntityFrameworkCore;
using VirtualRoulette.Models.Entities;

namespace VirtualRoulette.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Bet> Bets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            return;
        }

        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
    }

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
                .IsRequired();
        });

        // Configure Bet entity
        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Configure foreign key relationship to User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure properties
            entity.Property(e => e.BetString)
                .IsRequired();
            
            entity.Property(e => e.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            // Add indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SpinId)
                .IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
