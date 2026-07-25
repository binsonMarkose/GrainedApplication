using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Fundraising;

public record CampaignListItemDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    decimal? TargetAmount,
    decimal Raised,
    Guid? LogoImageId,
    bool IsPublished,
    bool IsActive,
    int DonationCount);

public record CampaignDetailDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    string? Description,
    decimal? TargetAmount,
    decimal Raised,
    Guid? LogoImageId,
    bool IsPublished,
    bool IsActive);

public class CampaignFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0, 10_000_000, ErrorMessage = "Target must be between 0 and 10,000,000")]
    public decimal? TargetAmount { get; set; }
}
