namespace Grained.Domain.Entities;

public class EventTicketType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    // e.g. "Adult", "Student", "Child", "Senior citizen".
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Preserves the order the admin arranged the ticket rows in.
    public int SortOrder { get; set; }
}
