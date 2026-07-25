using Grained.Domain.Common;
using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

// A message written by a ChurchAdmin and delivered to teachers and/or parents. Recipients see it
// as a pop-up on their next login and in their Announcements tab until they dismiss it.
public class Announcement : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;

    // Snapshot of the author so the recipient view doesn't depend on the user record surviving.
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public ICollection<AnnouncementReceipt> Receipts { get; set; } = new List<AnnouncementReceipt>();
}
