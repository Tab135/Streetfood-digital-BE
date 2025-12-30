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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Phone_number).HasMaxLength(20);
            entity.Property(e => e.Avatar_url).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(100);
        });

        modelBuilder.Entity<OtpVerify>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Otp).HasMaxLength(6);
        });
    }
}
