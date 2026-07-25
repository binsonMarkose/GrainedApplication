using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Public;

// ---- Storefront ----
public record PublicChurchDto(
    string Slug,
    string Name,
    List<PublicEventListItemDto> Events,
    List<PublicCampaignListItemDto> Campaigns);

public record PublicCampaignListItemDto(
    Guid Id,
    string Title,
    decimal? TargetAmount,
    decimal Raised,
    Guid? LogoImageId);

public record PublicEventListItemDto(
    Guid Id,
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    string? Location,
    decimal? FromPrice);

// ---- Single event ----
public record PublicTicketTypeDto(Guid Id, string Name, decimal Price);

public record PublicEventDto(
    Guid Id,
    string ChurchName,
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    string? Location,
    string? Description,
    bool EnableTshirt,
    List<PublicTicketTypeDto> TicketTypes);

// ---- Registration ----
public class RegisterSelectionModel
{
    public Guid TicketTypeId { get; set; }

    [Range(0, 1000, ErrorMessage = "Quantity must be between 0 and 1000")]
    public int Quantity { get; set; }
}

public class EventRegistrationModel : IValidatableObject
{
    [Required(ErrorMessage = "Your name is required")]
    [MaxLength(200)]
    public string PurchaserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    [MaxLength(256)]
    public string PurchaserEmail { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? PurchaserPhone { get; set; }

    [MaxLength(20)]
    public string? TshirtSize { get; set; }

    public List<RegisterSelectionModel> Selections { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Selections.Any(s => s.Quantity > 0))
        {
            yield return new ValidationResult("Select at least one ticket", [nameof(Selections)]);
        }
    }
}

public record RegistrationResultDto(
    Guid RegistrationId,
    decimal Total,
    string Currency,
    string Status,
    string Reference);

// ---- Public campaign + donation ----
public record PublicCampaignDto(
    Guid Id,
    string ChurchName,
    string Title,
    string? Description,
    decimal? TargetAmount,
    decimal Raised,
    Guid? LogoImageId);

public class DonationModel
{
    [Range(1, 1_000_000, ErrorMessage = "Enter an amount of at least £1")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Your name is required")]
    [MaxLength(200)]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    [MaxLength(256)]
    public string DonorEmail { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    public bool IsNamePublic { get; set; } = true;
}

public record DonationResultDto(
    Guid DonationId,
    decimal Amount,
    decimal Raised,
    string Status,
    string Reference);
