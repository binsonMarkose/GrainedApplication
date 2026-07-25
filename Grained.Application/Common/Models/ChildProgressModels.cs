namespace Grained.Application.Common.Models;

public record ChildProgressDto(
    Guid ChildId,
    Guid LessonId,
    string LessonTitle,
    DateTime? CompletedAtUtc,
    int? QuizScore,
    bool MemoryVerseCompleted,
    bool ActivityCompleted,
    bool PrayerCompleted);

public class ChildProgressUpdateModel
{
    public Guid ChildId { get; set; }
    public Guid LessonId { get; set; }
    public int? QuizScore { get; set; }
    public bool MemoryVerseCompleted { get; set; }
    public bool ActivityCompleted { get; set; }
    public bool PrayerCompleted { get; set; }
}
