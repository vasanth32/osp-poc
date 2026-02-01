using System.ComponentModel.DataAnnotations;

namespace FeeManagementService.Models;

public class CreateFeeRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public string FeeType { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }
}

