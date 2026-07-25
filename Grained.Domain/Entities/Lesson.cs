using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string BibleReference { get; set; } = string.Empty;
    public string? Theme { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public string StoryContent { get; set; } = string.Empty;
    public string? LearningObjective { get; set; }
    public string? Activity { get; set; }
    public string? Prayer { get; set; }

    // Authoring lifecycle. Status is authoritative; IsPublished mirrors (Status == Published) so all
    // existing "is this lesson live?" reads keep working. Both are only ever changed together, in
    // LessonService — never set one without the other.
    public LessonStatus Status { get; set; } = LessonStatus.Draft;
    public bool IsPublished { get; set; }

    // Who authored this lesson (a teacher or an admin). Name is snapshotted for display/attribution.
    public Guid? AuthorUserId { get; set; }
    public string? AuthorName { get; set; }

    // Admin's note when a submitted lesson is sent back for changes (cleared on publish).
    public string? ReviewNote { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    // Cross-church library lineage (Phase 2 — copy-on-import). Null for lessons authored in place.
    // SourceLessonId = the library lesson this was copied from; OriginChurchId = the authoring church.
    public Guid? SourceLessonId { get; set; }
    public Guid? OriginChurchId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public MemoryVerse? MemoryVerse { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<LessonClassGroup> AssignedClassGroups { get; set; } = new List<LessonClassGroup>();
    public ICollection<ChildProgress> ChildProgresses { get; set; } = new List<ChildProgress>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
