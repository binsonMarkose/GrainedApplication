using Grained.Domain.Common;

namespace Grained.Domain.Entities;

public class Event : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }

    // Shown on the public event page.
    public string? Description { get; set; }

    // "Add a T-shirt" toggle — when on, a T-shirt add-on is offered at registration.
    public bool EnableTshirt { get; set; }

    // Show on the event page vs. keep as a draft.
    public bool IsPublished { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // Priced attendee categories, e.g. Adult / Student / Child / Senior citizen (+ any custom).
    public ICollection<EventTicketType> TicketTypes { get; set; } = new List<EventTicketType>();
}
