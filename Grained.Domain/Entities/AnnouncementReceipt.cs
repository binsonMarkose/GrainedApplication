namespace Grained.Domain.Entities;

// One row per recipient who has read/dismissed an announcement. Absence of a row = unread.
public class AnnouncementReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;
}
