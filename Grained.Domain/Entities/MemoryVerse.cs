namespace Grained.Domain.Entities;

public class MemoryVerse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public string VerseText { get; set; } = string.Empty;
    public string BibleReference { get; set; } = string.Empty;
    public string? ShortExplanation { get; set; }
}
