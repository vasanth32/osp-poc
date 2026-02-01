using Microsoft.EntityFrameworkCore;

namespace FeeManagementService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add DbSet properties for your entities here
    // Example:
    // public DbSet<Fee> Fees { get; set; }
}

