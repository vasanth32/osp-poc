using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeManagementService.Models;

public class Fee
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SchoolId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public FeeType FeeType { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public FeeStatus Status { get; set; } = FeeStatus.Active;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

