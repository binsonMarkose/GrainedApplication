using System.ComponentModel.DataAnnotations;
using Grained.Domain.Enums;

namespace Grained.Application.Announcements;

// ---- Admin (author) view ----
public record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    AnnouncementAudience Audience,
    string AudienceLabel,
    string CreatedByName,
    DateTime CreatedAtUtc,
    bool IsActive,
    int ReadCount);

public class AnnouncementFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "A title is required")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "A message is required")]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;
}

// ---- Recipient (teacher / parent) inbox view ----
public record InboxAnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    string AudienceLabel,
    string CreatedByName,
    DateTime CreatedAtUtc,
    bool IsRead);
