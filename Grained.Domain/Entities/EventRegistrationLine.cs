namespace Grained.Domain.Entities;

// One ticket-type line on a registration. Name + unit price are snapshotted at purchase time so
// the record stays correct even if the event's ticket types are later edited or removed (which is
// why there is deliberately no FK back to EventTicketType).
public class EventRegistrationLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventRegistrationId { get; set; }
    public EventRegistration EventRegistration { get; set; } = null!;

    public Guid EventTicketTypeId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
