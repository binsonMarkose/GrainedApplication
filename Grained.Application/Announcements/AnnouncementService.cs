using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Announcements;

public class AnnouncementService(IApplicationDbContext db) : IAnnouncementService
{
    private static string AudienceLabel(AnnouncementAudience a) => a switch
    {
        AnnouncementAudience.Teachers => "Teachers",
        AnnouncementAudience.Parents => "Parents",
        _ => "Everyone",
    };

    // The audiences a user is entitled to receive, given their roles. Recipients are teachers and
    // parents — an "Everyone" broadcast reaches both. A pure ChurchAdmin/SuperAdmin is a sender,
    // not a recipient, so they receive nothing (empty list → no rows match).
    private static List<AnnouncementAudience> AudiencesFor(bool isTeacher, bool isParent)
    {
        var list = new List<AnnouncementAudience>();
        if (!isTeacher && !isParent) return list;
        list.Add(AnnouncementAudience.Everyone);
        if (isTeacher) list.Add(AnnouncementAudience.Teachers);
        if (isParent) list.Add(AnnouncementAudience.Parents);
        return list;
    }

    // ---- Author (ChurchAdmin) ----

    public async Task<List<AnnouncementDto>> GetForChurchAsync(Guid churchId, CancellationToken ct = default)
    {
        // Project to the raw fields in SQL, then map the audience label in memory (AudienceLabel
        // is a C# method EF can't translate).
        var rows = await db.Announcements.AsNoTracking()
            .Where(a => a.ChurchId == churchId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new { a.Id, a.Title, a.Body, a.Audience, a.CreatedByName, a.CreatedAtUtc, a.IsActive, ReadCount = a.Receipts.Count })
            .ToListAsync(ct);

        return rows.Select(a => new AnnouncementDto(
            a.Id, a.Title, a.Body, a.Audience, AudienceLabel(a.Audience),
            a.CreatedByName, a.CreatedAtUtc, a.IsActive, a.ReadCount)).ToList();
    }

    public async Task<Guid> CreateAsync(Guid churchId, Guid authorUserId, string authorName, AnnouncementFormModel model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            throw new ValidationException("A title is required.");
        if (string.IsNullOrWhiteSpace(model.Body))
            throw new ValidationException("A message is required.");

        var announcement = new Announcement
        {
            ChurchId = churchId,
            Title = model.Title.Trim(),
            Body = model.Body.Trim(),
            Audience = model.Audience,
            CreatedByUserId = authorUserId,
            CreatedByName = authorName,
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);
        return announcement.Id;
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id && a.ChurchId == churchId, ct)
            ?? throw new ValidationException("Announcement not found.");
        announcement.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    // ---- Recipient (teacher / parent) ----

    public async Task<List<InboxAnnouncementDto>> GetInboxAsync(Guid churchId, Guid userId, bool isTeacher, bool isParent, CancellationToken ct = default)
    {
        var audiences = AudiencesFor(isTeacher, isParent);

        var rows = await db.Announcements.AsNoTracking()
            .Where(a => a.ChurchId == churchId && a.IsActive && audiences.Contains(a.Audience))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new { a.Id, a.Title, a.Body, a.Audience, a.CreatedByName, a.CreatedAtUtc, IsRead = a.Receipts.Any(r => r.UserId == userId) })
            .ToListAsync(ct);

        return rows.Select(a => new InboxAnnouncementDto(
            a.Id, a.Title, a.Body, AudienceLabel(a.Audience), a.CreatedByName, a.CreatedAtUtc, a.IsRead)).ToList();
    }

    public async Task MarkReadAsync(Guid announcementId, Guid churchId, Guid userId, CancellationToken ct = default)
    {
        var deliverable = await db.Announcements.AsNoTracking()
            .AnyAsync(a => a.Id == announcementId && a.ChurchId == churchId, ct);
        if (!deliverable)
            throw new ValidationException("Announcement not found.");

        var already = await db.AnnouncementReceipts
            .AnyAsync(r => r.AnnouncementId == announcementId && r.UserId == userId, ct);
        if (already) return;

        db.AnnouncementReceipts.Add(new AnnouncementReceipt { AnnouncementId = announcementId, UserId = userId });
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid churchId, Guid userId, bool isTeacher, bool isParent, CancellationToken ct = default)
    {
        var audiences = AudiencesFor(isTeacher, isParent);

        var unreadIds = await db.Announcements.AsNoTracking()
            .Where(a => a.ChurchId == churchId && a.IsActive && audiences.Contains(a.Audience)
                && !a.Receipts.Any(r => r.UserId == userId))
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (unreadIds.Count == 0) return;

        foreach (var id in unreadIds)
            db.AnnouncementReceipts.Add(new AnnouncementReceipt { AnnouncementId = id, UserId = userId });

        await db.SaveChangesAsync(ct);
    }
}
