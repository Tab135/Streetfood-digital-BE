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
    public DbSet<Tier> Tiers { get; set; }
    public DbSet<BranchImage> BranchImages { get; set; }
    public DbSet<BranchRegisterRequest> BranchRegisterRequests { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<DayOff> DayOffs { get; set; }

    // Feedback-related DbSets
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<FeedbackImage> FeedbackImages { get; set; }
    public DbSet<FeedbackTag> FeedbackTags { get; set; }
    public DbSet<FeedbackTagAssociation> FeedbackTagAssociations { get; set; }

    // Flow 2: Review & Rating enhancements
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDish> OrderDishes { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<FeedbackVote> FeedbackVotes { get; set; }
    public DbSet<VendorReply> VendorReplies { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    // Menu Management DbSets
    public DbSet<Category> Categories { get; set; }
    public DbSet<Taste> Tastes { get; set; }
    public DbSet<Dish> Dishes { get; set; }
    public DbSet<DishTaste> DishTastes { get; set; }
    public DbSet<VendorDietaryPreference> VendorDietaryPreferences { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<BranchDish> BranchDishes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Tier Configuration ---
        modelBuilder.Entity<Branch>()
            .HasOne(b => b.Tier)
            .WithMany(t => t.Branches)
            .HasForeignKey(b => b.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tier>().HasData(
            new Tier { TierId = 1, Name = "Warning", Weight = 0.5 },
            new Tier { TierId = 2, Name = "Silver", Weight = 1.0 },
            new Tier { TierId = 3, Name = "Gold", Weight = 1.5 },
            new Tier { TierId = 4, Name = "Diamond", Weight = 2.0 }
        );

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(100);
            entity.Property(e => e.MoneyBalance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
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
            entity.Property(e => e.MoneyBalance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

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

            entity.HasOne(e => e.Manager)
                  .WithMany()
                  .HasForeignKey(e => e.ManagerId)
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

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.Dishes)
                .HasForeignKey(e => e.VendorId)
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

        // VendorDietaryPreference
        modelBuilder.Entity<VendorDietaryPreference>(entity =>
        {
            entity.HasKey(e => e.VendorDietaryPreferenceId);

            entity.HasOne(e => e.Vendor)
                  .WithMany(v => v.VendorDietaryPreferences)
                  .HasForeignKey(e => e.VendorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DietaryPreference)
                  .WithMany(dp => dp.VendorDietaryPreferences)
                  .HasForeignKey(e => e.DietaryPreferenceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== FLOW 2: REVIEW & RATING ENTITIES ====================

        // Order (minimal for Flow 2)
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(255).IsRequired();
            entity.Property(e => e.Table).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(255);
            entity.Property(e => e.CompletionCode).HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                  .WithMany()
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

          modelBuilder.Entity<OrderDish>(entity =>
          {
            entity.HasKey(e => new { e.OrderId, e.DishId });
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderDishes)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BranchDish)
                .WithMany(bd => bd.OrderDishes)
                .HasForeignKey(e => new { e.BranchId, e.DishId })
                .OnDelete(DeleteBehavior.Restrict);
          });

              modelBuilder.Entity<Cart>(entity =>
              {
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.UserId).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Branch)
                    .WithMany()
                    .HasForeignKey(e => e.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
              });

              modelBuilder.Entity<CartItem>(entity =>
              {
                entity.HasKey(e => e.CartItemId);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => new { e.CartId, e.DishId }).IsUnique();

                entity.HasOne(e => e.Cart)
                    .WithMany(c => c.Items)
                    .HasForeignKey(e => e.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Dish)
                    .WithMany()
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Restrict);
              });

        // Feedback → Order FK
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Feedbacks)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // FeedbackVote
        modelBuilder.Entity<FeedbackVote>(entity =>
        {
            entity.HasKey(e => e.FeedbackVoteId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.FeedbackId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Feedback)
                  .WithMany(f => f.Votes)
                  .HasForeignKey(e => e.FeedbackId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // VendorReply
        modelBuilder.Entity<VendorReply>(entity =>
        {
            entity.HasKey(e => e.VendorReplyId);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.FeedbackId).IsUnique();

            entity.HasOne(e => e.Feedback)
                  .WithOne(f => f.VendorReply)
                  .HasForeignKey<VendorReply>(e => e.FeedbackId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.UserId, e.IsRead });

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Order)
                  .WithMany()
                  .HasForeignKey(p => p.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Branch metrics defaults
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(e => e.TotalReviewCount).HasDefaultValue(0);
            entity.Property(e => e.TotalRatingSum).HasDefaultValue(0);
        });
        // BranchDish
        modelBuilder.Entity<BranchDish>(entity =>
        {
            entity.HasKey(e => new { e.BranchId, e.DishId });
            entity.Property(e => e.IsSoldOut).HasDefaultValue(false);

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.BranchDishes)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Dish)
                  .WithMany(d => d.BranchDishes)
                  .HasForeignKey(e => e.DishId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}