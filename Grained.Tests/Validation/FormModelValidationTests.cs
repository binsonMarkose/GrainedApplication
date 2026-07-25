using System.ComponentModel.DataAnnotations;
using Grained.Application.ClassGroups;
using Grained.Application.Lessons;
using Grained.Domain.Enums;

namespace Grained.Tests.Validation;

public class FormModelValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ClassGroupFormModel_MaxAgeLessThanMinAge_FailsValidation()
    {
        var model = new ClassGroupFormModel { Name = "Toddlers", MinAge = 10, MaxAge = 5 };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassGroupFormModel.MaxAge)));
    }

    [Fact]
    public void ClassGroupFormModel_ValidAgeRange_PassesValidation()
    {
        var model = new ClassGroupFormModel { Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void ClassGroupFormModel_MissingName_FailsValidation()
    {
        var model = new ClassGroupFormModel { Name = "", MinAge = 5, MaxAge = 10 };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassGroupFormModel.Name)));
    }

    [Fact]
    public void QuizQuestionFormModel_NoCorrectOption_FailsValidation()
    {
        var model = new QuizQuestionFormModel
        {
            QuestionText = "Who?",
            QuestionType = QuestionType.SingleChoice,
            Points = 1,
            Options = [new QuizOptionFormModel { OptionText = "Nobody", IsCorrect = false }]
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(QuizQuestionFormModel.Options)));
    }

    [Fact]
    public void QuizQuestionFormModel_WithCorrectOption_PassesValidation()
    {
        var model = new QuizQuestionFormModel
        {
            QuestionText = "Who?",
            QuestionType = QuestionType.SingleChoice,
            Points = 1,
            Options = [new QuizOptionFormModel { OptionText = "God", IsCorrect = true }]
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void ChurchFormModel_InvalidEmail_FailsValidation()
    {
        var model = new Grained.Application.Churches.ChurchFormModel
        {
            Name = "Grace Church",
            Email = "not-an-email"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Grained.Application.Churches.ChurchFormModel.Email)));
    }
}
