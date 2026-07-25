using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

public class QuizQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int Points { get; set; } = 1;

    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}
