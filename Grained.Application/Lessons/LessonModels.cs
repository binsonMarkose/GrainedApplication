using System.ComponentModel.DataAnnotations;
using Grained.Domain.Enums;

namespace Grained.Application.Lessons;

public record LessonListItemDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    string BibleReference,
    string? Theme,
    string AgeGroup,
    bool IsPublished,
    LessonStatus Status,
    Guid? AuthorUserId,
    string? AuthorName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    bool HasMemoryVerse,
    int QuestionCount,
    List<string> AssignedClassGroupNames,
    List<Guid> AssignedClassGroupIds,
    // Most recent time this lesson was marked completed for a child in the caller's scope (a teacher's
    // assigned classes; church-wide for admins). Null = never taught yet.
    DateTime? LastCompletedAtUtc);

public record MemoryVerseDto(string VerseText, string BibleReference, string? ShortExplanation);

public record QuizOptionDto(Guid Id, string OptionText, bool IsCorrect);

public record QuizQuestionDto(Guid Id, string QuestionText, QuestionType QuestionType, int Points, List<QuizOptionDto> Options);

public record QuizDto(Guid Id, string Title, string? Description, List<QuizQuestionDto> Questions);

public record LessonDetailDto(
    Guid Id,
    Guid ChurchId,
    string Title,
    string BibleReference,
    string? Theme,
    string AgeGroup,
    string StoryContent,
    string? LearningObjective,
    string? Activity,
    string? Prayer,
    bool IsPublished,
    LessonStatus Status,
    Guid? AuthorUserId,
    string? AuthorName,
    string? ReviewNote,
    MemoryVerseDto? MemoryVerse,
    QuizDto? Quiz,
    List<Guid> AssignedClassGroupIds,
    DateTime? LastCompletedAtUtc);

public class LessonFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bible reference is required")]
    [MaxLength(100)]
    public string BibleReference { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Theme { get; set; }

    [Required(ErrorMessage = "Age group is required")]
    [MaxLength(50)]
    public string AgeGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "Story content is required")]
    public string StoryContent { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LearningObjective { get; set; }

    [MaxLength(1000)]
    public string? Activity { get; set; }

    [MaxLength(1000)]
    public string? Prayer { get; set; }

    public MemoryVerseFormModel MemoryVerse { get; set; } = new();
}

public class MemoryVerseFormModel
{
    [MaxLength(1000)]
    public string? VerseText { get; set; }

    [MaxLength(100)]
    public string? BibleReference { get; set; }

    [MaxLength(500)]
    public string? ShortExplanation { get; set; }

    // A memory verse counts as provided once the verse text is entered; its reference is optional and
    // falls back to the lesson's Bible reference (see LessonService) so a typed verse is never dropped.
    public bool IsProvided => !string.IsNullOrWhiteSpace(VerseText);
}

public class QuizOptionFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Option text is required")]
    [MaxLength(500)]
    public string OptionText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

public class QuizQuestionFormModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Question text is required")]
    public string QuestionText { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;

    [Range(1, 100, ErrorMessage = "Points must be between 1 and 100")]
    public int Points { get; set; } = 1;

    public List<QuizOptionFormModel> Options { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Options.Count == 0)
        {
            yield return new ValidationResult("At least one option is required", [nameof(Options)]);
        }
        else if (!Options.Any(o => o.IsCorrect))
        {
            yield return new ValidationResult("At least one option must be marked correct", [nameof(Options)]);
        }
    }
}
