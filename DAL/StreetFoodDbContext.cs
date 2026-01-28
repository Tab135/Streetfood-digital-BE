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

    // Dietary preferences
    public DbSet<DietaryPreference> DietaryPreferences { get; set; }
    public DbSet<UserDietaryPreference> UserDietaryPreferences { get; set; }

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

        // Dietary preference entity and seed data
        modelBuilder.Entity<DietaryPreference>(entity =>
        {
            entity.HasKey(e => e.DietaryPreferenceId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.HasData(
                new DietaryPreference { DietaryPreferenceId = 1, Name = "Ăn chay", Description = "Không thịt" },
                new DietaryPreference { DietaryPreferenceId = 2, Name = "Cay", Description = "Món ăn có vị cay nồng, sử dụng nhiều ớt hoặc tiêu" },
                new DietaryPreference { DietaryPreferenceId = 3, Name = "Ngọt", Description = "Món ăn có vị ngọt, hoặc các món tráng miệng" },
                new DietaryPreference { DietaryPreferenceId = 4, Name = "Mặn", Description = "Hương vị đậm đà, thích hợp ăn kèm với cơm" },
                new DietaryPreference { DietaryPreferenceId = 5, Name = "Hải sản", Description = "Bao gồm các loại tôm, cua, cá, mực và đồ biển khác" }
            );
        });

        modelBuilder.Entity<UserDietaryPreference>(entity =>
        {
            entity.HasKey(e => e.UserDietaryPreferenceId);
            entity.HasOne(udp => udp.User).WithMany(u => u.DietaryPreferences).HasForeignKey(udp => udp.UserId);
            entity.HasOne(udp => udp.DietaryPreference).WithMany(dp => dp.UserPreferences).HasForeignKey(udp => udp.DietaryPreferenceId);
        });
    }
}
