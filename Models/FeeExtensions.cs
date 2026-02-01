namespace FeeManagementService.Models;

public static class FeeExtensions
{
    public static FeeResponse ToResponse(this Fee fee)
    {
        return new FeeResponse
        {
            Id = fee.Id,
            SchoolId = fee.SchoolId,
            Title = fee.Title,
            Description = fee.Description,
            Amount = fee.Amount,
            FeeType = fee.FeeType,
            ImageUrl = fee.ImageUrl,
            Status = fee.Status,
            CreatedBy = fee.CreatedBy,
            CreatedAt = fee.CreatedAt,
            UpdatedBy = fee.UpdatedBy,
            UpdatedAt = fee.UpdatedAt
        };
    }
}

