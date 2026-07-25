using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Events;

public record EventTicketTypeDto(Guid Id, string Name, decimal Price);

public record EventListItemDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    string? Location,
    bool EnableTshirt,
    bool IsPublished,
    bool IsActive,
    int TicketTypeCount);

public record EventDetailDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    string? Location,
    string? Description,
    bool EnableTshirt,
    bool IsPublished,
    bool IsActive,
    List<EventTicketTypeDto> TicketTypes);

public class EventTicketTypeFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Ticket type name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100,000")]
    public decimal Price { get; set; }
}

public class EventFormModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool EnableTshirt { get; set; }

    public List<EventTicketTypeFormModel> TicketTypes { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "End date must be on or after the start date", [nameof(EndDate)]);
        }

        if (TicketTypes.Count == 0)
        {
            yield return new ValidationResult(
                "Add at least one ticket type", [nameof(TicketTypes)]);
        }
    }
}
