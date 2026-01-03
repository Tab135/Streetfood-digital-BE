using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class StreetFoodDbContext : DbContext
{
    public StreetFoodDbContext(DbContextOptions<StreetFoodDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OtpVerify> OtpVerifies { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(100);
        });

        modelBuilder.Entity<OtpVerify>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Otp).HasMaxLength(6);
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.BadgeId);
            entity.Property(e => e.BadgeName).HasMaxLength(100);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BadgeId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
