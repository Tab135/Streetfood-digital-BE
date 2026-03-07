using BO.DTO.Payments;
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

    // Vendor-related DbSets
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<BranchImage> BranchImages { get; set; }
    public DbSet<BranchRegisterRequest> BranchRegisterRequests { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<DayOff> DayOffs { get; set; }

    // Feedback-related DbSets
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<FeedbackImage> FeedbackImages { get; set; }
    public DbSet<FeedbackTag> FeedbackTags { get; set; }
    public DbSet<FeedbackTagAssociation> FeedbackTagAssociations { get; set; }

    // Menu Management DbSets
    public DbSet<Category> Categories { get; set; }
    public DbSet<Taste> Tastes { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<DishTaste> DishTastes { get; set; }
    public DbSet<DishDietaryPreference> DishDietaryPreferences { get; set; }
    public DbSet<Payment> Payments { get; set; }

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

        // Vendor-related entities
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasKey(e => e.VendorId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.VendorOwner)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.City).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Vendor)
                  .WithMany(v => v.Branches)
                  .HasForeignKey(e => e.VendorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BranchImage>(entity =>
        {
            entity.HasKey(e => e.BranchImageId);
            entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.BranchImages)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(e => e.WorkScheduleId);
            entity.Property(e => e.Weekday).IsRequired();
            entity.Property(e => e.OpenTime).IsRequired();
            entity.Property(e => e.CloseTime).IsRequired();
            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.WorkSchedules)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DayOff>(entity =>
        {
            entity.HasKey(e => e.DayOffId);
            entity.Property(e => e.StartDate).HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.DayOffs)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchRegisterRequest>(entity =>
        {
            entity.HasKey(e => e.BranchRegisterRequestId);
            entity.Property(e => e.LicenseUrl).IsRequired(false);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Branch)
                  .WithOne()
                  .HasForeignKey<BranchRegisterRequest>(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback entities
        modelBuilder.Entity<FeedbackTag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId);
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Branch)
                  .WithMany()
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeedbackImage>(entity =>
        {
            entity.HasKey(e => e.FeedbackImageId);
            entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Feedback)
                  .WithMany(f => f.FeedbackImages)
                  .HasForeignKey(e => e.FeedbackId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeedbackTagAssociation>(entity =>
        {
            entity.HasKey(e => e.FeedbackTagId);
            entity.HasOne(e => e.Feedback)
                  .WithMany(f => f.FeedbackTagAssociations)
                  .HasForeignKey(e => e.FeedbackId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.FeedbackTag)
                  .WithMany(t => t.FeedbackTagAssociations)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== MENU MANAGEMENT ENTITIES ====================

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Taste
        modelBuilder.Entity<Taste>(entity =>
        {
            entity.HasKey(e => e.TasteId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Dish
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(e => e.DishId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsSoldOut).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.Dishes)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Dishes)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DishTaste
        modelBuilder.Entity<DishTaste>(entity =>
        {
            entity.HasKey(e => e.DishTasteId);

            entity.HasOne(e => e.Dish)
                  .WithMany(d => d.DishTastes)
                  .HasForeignKey(e => e.DishId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Taste)
                  .WithMany(t => t.DishTastes)
                  .HasForeignKey(e => e.TasteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DishDietaryPreference
        modelBuilder.Entity<DishDietaryPreference>(entity =>
        {
            entity.HasKey(e => e.DishDietaryPreferenceId);

            entity.HasOne(e => e.Dish)
                  .WithMany(d => d.DishDietaryPreferences)
                  .HasForeignKey(e => e.DishId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DietaryPreference)
                  .WithMany(dp => dp.DishDietaryPreferences)
                  .HasForeignKey(e => e.DietaryPreferenceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}