using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Services;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Tests.Common;

namespace Grained.Tests.Lessons;

public class LessonServiceTests
{
    // These tests act as a ChurchAdmin (the FakeCurrentUserService default), who can author any lesson.
    private static readonly Guid TestAuthorId = Guid.NewGuid();

    private static async Task<(LessonService Service, Guid ChurchId)> CreateServiceWithChurchAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Test Church", Email = "test@church.org" };
        db.Churches.Add(church);
        await db.SaveChangesAsync();
        var currentUser = new FakeCurrentUserService { UserId = TestAuthorId, ChurchId = church.Id };
        return (new LessonService(db, new TeacherScope(db, currentUser), currentUser), church.Id);
    }

    private static LessonFormModel ValidLessonForm() => new()
    {
        Title = "Creation",
        BibleReference = "Genesis 1",
        AgeGroup = "Ages 5-10",
        StoryContent = "In the beginning...",
        MemoryVerse = new MemoryVerseFormModel
        {
            VerseText = "In the beginning God created the heaven and the earth.",
            BibleReference = "Genesis 1:1"
        }
    };

    private static QuizQuestionFormModel ValidQuestionForm() => new()
    {
        QuestionText = "On what day did God create light?",
        QuestionType = QuestionType.SingleChoice,
        Points = 1,
        Options =
        [
            new QuizOptionFormModel { OptionText = "Day 1", IsCorrect = true },
            new QuizOptionFormModel { OptionText = "Day 6", IsCorrect = false }
        ]
    };

    [Fact]
    public async Task CreateAsync_CreatesLessonWithQuizShellAndMemoryVerse()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();

        var id = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());

        var detail = await service.GetDetailAsync(id, churchId);
        Assert.NotNull(detail);
        Assert.Equal("Creation", detail!.Title);
        Assert.NotNull(detail.MemoryVerse);
        Assert.NotNull(detail.Quiz);
        Assert.Empty(detail.Quiz!.Questions);
        Assert.False(detail.IsPublished);
    }

    [Fact]
    public async Task CreateAsync_MemoryVerseWithoutOwnReference_SavesAndFallsBackToLessonReference()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var form = ValidLessonForm();
        form.BibleReference = "Genesis 1";
        // Author types the verse text but leaves the memory verse's own reference blank.
        form.MemoryVerse = new MemoryVerseFormModel { VerseText = "In the beginning God created the heaven and the earth." };

        var id = await service.CreateAsync(churchId, TestAuthorId, "Author", form);

        var detail = await service.GetDetailAsync(id, churchId);
        Assert.NotNull(detail!.MemoryVerse);
        Assert.Equal("In the beginning God created the heaven and the earth.", detail.MemoryVerse!.VerseText);
        Assert.Equal("Genesis 1", detail.MemoryVerse.BibleReference); // fell back to the lesson reference
    }

    [Fact]
    public async Task AddOrUpdateQuestionAsync_AddsNewQuestionWithOptions()
    {
        // Regression test: adding a question to an already-tracked Lesson must go through
        // db.QuizQuestions.Add/db.QuizOptions.AddRange explicitly, not just navigation-collection
        // mutation, otherwise EF Core mismarks the new rows as Modified and SaveChanges throws
        // DbUpdateConcurrencyException ("expected to affect 1 row(s), but actually affected 0").
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());

        var questionId = await service.AddOrUpdateQuestionAsync(lessonId, churchId, ValidQuestionForm());

        var detail = await service.GetDetailAsync(lessonId, churchId);
        var question = Assert.Single(detail!.Quiz!.Questions);
        Assert.Equal(questionId, question.Id);
        Assert.Equal(2, question.Options.Count);
        Assert.Contains(question.Options, o => o.OptionText == "Day 1" && o.IsCorrect);
    }

    [Fact]
    public async Task AddOrUpdateQuestionAsync_UpdatingExistingQuestion_ReplacesOptions()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());
        var questionId = await service.AddOrUpdateQuestionAsync(lessonId, churchId, ValidQuestionForm());

        var updateModel = ValidQuestionForm();
        updateModel.Id = questionId;
        updateModel.Options =
        [
            new QuizOptionFormModel { OptionText = "Replaced Option", IsCorrect = true }
        ];

        await service.AddOrUpdateQuestionAsync(lessonId, churchId, updateModel);

        var detail = await service.GetDetailAsync(lessonId, churchId);
        var question = Assert.Single(detail!.Quiz!.Questions);
        var option = Assert.Single(question.Options);
        Assert.Equal("Replaced Option", option.OptionText);
    }

    [Fact]
    public async Task AddOrUpdateQuestionAsync_NoCorrectAnswer_ThrowsValidationException()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());

        var model = new QuizQuestionFormModel
        {
            QuestionText = "Bad question",
            Options = [new QuizOptionFormModel { OptionText = "Only option", IsCorrect = false }]
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.AddOrUpdateQuestionAsync(lessonId, churchId, model));
    }

    [Fact]
    public async Task PublishAsync_WithoutMemoryVerse_ThrowsValidationException()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var form = ValidLessonForm();
        form.MemoryVerse = new MemoryVerseFormModel();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", form);
        await service.AddOrUpdateQuestionAsync(lessonId, churchId, ValidQuestionForm());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PublishAsync(lessonId, churchId));
        Assert.Contains("memory verse", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublishAsync_WithoutQuizQuestions_ThrowsValidationException()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.PublishAsync(lessonId, churchId));
        Assert.Contains("quiz question", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublishAsync_WithMemoryVerseAndQuestion_Succeeds()
    {
        var (service, churchId) = await CreateServiceWithChurchAsync();
        var lessonId = await service.CreateAsync(churchId, TestAuthorId, "Author", ValidLessonForm());
        await service.AddOrUpdateQuestionAsync(lessonId, churchId, ValidQuestionForm());

        await service.PublishAsync(lessonId, churchId);

        var detail = await service.GetDetailAsync(lessonId, churchId);
        Assert.True(detail!.IsPublished);
    }
}
