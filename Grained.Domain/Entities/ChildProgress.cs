namespace Grained.Domain.Entities;

public class ChildProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public DateTime? CompletedAtUtc { get; set; }
    public int? QuizScore { get; set; }
    public bool MemoryVerseCompleted { get; set; }
    public bool ActivityCompleted { get; set; }
    public bool PrayerCompleted { get; set; }
}
