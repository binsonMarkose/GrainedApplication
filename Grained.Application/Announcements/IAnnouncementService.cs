using Grained.Application.Announcements;

namespace Grained.Application.Announcements;

public interface IAnnouncementService
{
    // --- Author (ChurchAdmin) ---
    Task<List<AnnouncementDto>> GetForChurchAsync(Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, Guid authorUserId, string authorName, AnnouncementFormModel model, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);

    // --- Recipient (teacher / parent) ---
    // isTeacher / isParent describe the current user; delivery is filtered by the announcement's audience.
    Task<List<InboxAnnouncementDto>> GetInboxAsync(Guid churchId, Guid userId, bool isTeacher, bool isParent, CancellationToken ct = default);
    Task MarkReadAsync(Guid announcementId, Guid churchId, Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid churchId, Guid userId, bool isTeacher, bool isParent, CancellationToken ct = default);
}
