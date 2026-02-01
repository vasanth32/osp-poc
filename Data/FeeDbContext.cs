using Microsoft.EntityFrameworkCore;
using FeeManagementService.Models;

namespace FeeManagementService.Data;

public class FeeDbContext : DbContext
{
    public FeeDbContext(DbContextOptions<FeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fee> Fees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Fee entity
        modelBuilder.Entity<Fee>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Property configurations
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.FeeType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(FeeStatus.Active);

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.SchoolId)
                .HasDatabaseName("IX_Fees_SchoolId");

            entity.HasIndex(e => e.FeeType)
                .HasDatabaseName("IX_Fees_FeeType");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Fees_Status");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Fees_CreatedAt");

            // Check constraint for Amount > 0
            entity.ToTable(t => t.HasCheckConstraint("CK_Fees_Amount_Positive", "Amount > 0"));
        });
    }
}

