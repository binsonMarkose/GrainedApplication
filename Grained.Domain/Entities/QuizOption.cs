namespace Grained.Domain.Entities;

public class QuizOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizQuestionId { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = null!;

    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
