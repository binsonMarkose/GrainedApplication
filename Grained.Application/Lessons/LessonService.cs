using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Lessons;

public class LessonService(IApplicationDbContext db, ITeacherScope teacherScope, ICurrentUserService currentUser) : ILessonService
{
    private bool IsAdmin => currentUser.IsChurchAdmin || currentUser.IsSuperAdmin;

    // The memory verse's reference is optional; fall back to the lesson's Bible reference when blank.
    private static string MemoryVerseReference(LessonFormModel model) =>
        string.IsNullOrWhiteSpace(model.MemoryVerse.BibleReference)
            ? model.BibleReference.Trim()
            : model.MemoryVerse.BibleReference!.Trim();

    // Status is authoritative; IsPublished mirrors it. Only ever change the two together.
    private static void SetStatus(Lesson lesson, LessonStatus status)
    {
        lesson.Status = status;
        lesson.IsPublished = status == LessonStatus.Published;
    }

    // A teacher may only touch lessons they authored; an admin may touch any lesson in the church.
    private void EnsureCanAuthor(Lesson lesson)
    {
        if (IsAdmin) return;
        if (currentUser.IsTeacher && lesson.AuthorUserId == currentUser.UserId) return;
        throw new ValidationException("You can only edit lessons you created.");
    }

    // Called after a content edit. A teacher editing an already-published lesson sends it back to
    // review (it leaves the live set until an admin re-approves); admin edits don't change status.
    private void OnContentEdited(Lesson lesson)
    {
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        if (!IsAdmin && lesson.Status == LessonStatus.Published)
            SetStatus(lesson, LessonStatus.InReview);
    }

    public async Task<List<LessonListItemDto>> GetForChurchAsync(Guid churchId, bool? publishedOnly = null, LessonStatus? status = null, Guid? classGroupId = null, CancellationToken ct = default)
    {
        var query = db.Lessons.AsNoTracking()
            .Include(l => l.MemoryVerse)
            .Include(l => l.Quiz).ThenInclude(q => q!.Questions)
            .Include(l => l.AssignedClassGroups).ThenInclude(a => a.ClassGroup)
            .Where(l => l.ChurchId == churchId);

        if (publishedOnly is not null)
            query = query.Where(l => l.IsPublished == publishedOnly);

        if (status is not null)
            query = query.Where(l => l.Status == status);

        // Filter to one class group's curriculum (used by the admin per-group ordering view).
        if (classGroupId is Guid filterGroup)
            query = query.Where(l => l.AssignedClassGroups.Any(a => a.ClassGroupId == filterGroup));

        // A plain teacher sees lessons they authored (any status) plus published lessons assigned to a
        // class group they teach (their delivery set). Admins see everything.
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null)
        {
            var userId = currentUser.UserId;
            query = query.Where(l =>
                l.AuthorUserId == userId ||
                (l.IsPublished && l.AssignedClassGroups.Any(a => scope.Contains(a.ClassGroupId))));
        }

        var lessons = await query.ToListAsync(ct);
        // A single group → teaching order (SortOrder of that assignment); otherwise newest first.
        lessons = classGroupId is Guid gid
            ? lessons.OrderBy(l => l.AssignedClassGroups.First(a => a.ClassGroupId == gid).SortOrder).ThenBy(l => l.Title).ToList()
            : lessons.OrderByDescending(l => l.CreatedAtUtc).ToList();
        // In a single-group view "taught" means taught to *that* group; otherwise use the caller's scope.
        var completionScope = classGroupId is Guid cg ? new List<Guid> { cg } : scope;
        var completed = await CompletionDatesAsync(lessons.Select(l => l.Id).ToList(), completionScope, ct);

        return lessons.Select(l => new LessonListItemDto(
            l.Id, l.ChurchId, l.Title, l.BibleReference, l.Theme, l.AgeGroup, l.IsPublished,
            l.Status, l.AuthorUserId, l.AuthorName,
            l.CreatedAtUtc, l.UpdatedAtUtc, l.MemoryVerse is not null,
            l.Quiz?.Questions.Count ?? 0,
            l.AssignedClassGroups.Select(a => a.ClassGroup.Name).ToList(),
            l.AssignedClassGroups.Select(a => a.ClassGroupId).ToList(),
            completed.TryGetValue(l.Id, out var completedAt) ? completedAt : null)).ToList();
    }

    // Most recent completion timestamp per lesson, among children in scope (a teacher's classes; all
    // for admins). Powers the "Completed · date" indicator.
    private async Task<Dictionary<Guid, DateTime>> CompletionDatesAsync(
        List<Guid> lessonIds, List<Guid>? scope, CancellationToken ct)
    {
        if (lessonIds.Count == 0)
            return new Dictionary<Guid, DateTime>();

        var query = db.ChildProgresses.AsNoTracking()
            .Where(p => lessonIds.Contains(p.LessonId) && p.CompletedAtUtc != null);
        if (scope is not null)
            query = query.Where(p => scope.Contains(p.Child.ClassGroupId));

        var rows = await query
            .GroupBy(p => p.LessonId)
            .Select(g => new { LessonId = g.Key, Last = g.Max(p => p.CompletedAtUtc) })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.LessonId, x => x.Last!.Value);
    }

    public async Task<LessonDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var detailQuery = db.Lessons.AsNoTracking()
            .Include(l => l.MemoryVerse)
            .Include(l => l.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Options)
            .Include(l => l.AssignedClassGroups)
            .Where(l => l.Id == id && l.ChurchId == churchId);

        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null)
        {
            var userId = currentUser.UserId;
            detailQuery = detailQuery.Where(l =>
                l.AuthorUserId == userId ||
                (l.IsPublished && l.AssignedClassGroups.Any(a => scope.Contains(a.ClassGroupId))));
        }

        var lesson = await detailQuery.FirstOrDefaultAsync(ct);
        if (lesson is null)
            return null;

        var completed = await CompletionDatesAsync([lesson.Id], scope, ct);
        return ToDetailDto(lesson, completed.TryGetValue(lesson.Id, out var completedAt) ? completedAt : null);
    }

    public async Task<Guid> CreateAsync(Guid churchId, Guid authorUserId, string authorName, LessonFormModel model, CancellationToken ct = default)
    {
        var lesson = new Lesson
        {
            ChurchId = churchId,
            AuthorUserId = authorUserId,
            AuthorName = authorName,
            Status = LessonStatus.Draft,
            IsPublished = false,
            Title = model.Title.Trim(),
            BibleReference = model.BibleReference.Trim(),
            Theme = model.Theme?.Trim(),
            AgeGroup = model.AgeGroup.Trim(),
            StoryContent = model.StoryContent.Trim(),
            LearningObjective = model.LearningObjective?.Trim(),
            Activity = model.Activity?.Trim(),
            Prayer = model.Prayer?.Trim(),
            Quiz = new Quiz { Title = $"{model.Title.Trim()} Quiz" }
        };

        if (model.MemoryVerse.IsProvided)
        {
            lesson.MemoryVerse = new MemoryVerse
            {
                VerseText = model.MemoryVerse.VerseText!.Trim(),
                BibleReference = MemoryVerseReference(model),
                ShortExplanation = model.MemoryVerse.ShortExplanation?.Trim()
            };
        }

        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(ct);
        return lesson.Id;
    }

    public async Task UpdateAsync(Guid churchId, LessonFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Lesson id is required for update.");

        var lesson = await db.Lessons
            .Include(l => l.MemoryVerse)
            .FirstOrDefaultAsync(l => l.Id == model.Id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        EnsureCanAuthor(lesson);

        lesson.Title = model.Title.Trim();
        lesson.BibleReference = model.BibleReference.Trim();
        lesson.Theme = model.Theme?.Trim();
        lesson.AgeGroup = model.AgeGroup.Trim();
        lesson.StoryContent = model.StoryContent.Trim();
        lesson.LearningObjective = model.LearningObjective?.Trim();
        lesson.Activity = model.Activity?.Trim();
        lesson.Prayer = model.Prayer?.Trim();

        if (model.MemoryVerse.IsProvided)
        {
            if (lesson.MemoryVerse is null)
            {
                lesson.MemoryVerse = new MemoryVerse { LessonId = lesson.Id };
                db.MemoryVerses.Add(lesson.MemoryVerse);
            }
            lesson.MemoryVerse.VerseText = model.MemoryVerse.VerseText!.Trim();
            lesson.MemoryVerse.BibleReference = MemoryVerseReference(model);
            lesson.MemoryVerse.ShortExplanation = model.MemoryVerse.ShortExplanation?.Trim();
        }
        else if (lesson.MemoryVerse is not null)
        {
            db.MemoryVerses.Remove(lesson.MemoryVerse);
        }

        OnContentEdited(lesson);
        await db.SaveChangesAsync(ct);
    }

    public async Task SubmitForReviewAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        EnsureCanAuthor(lesson);

        if (lesson.Status == LessonStatus.Published)
            throw new ValidationException("This lesson is already published.");
        if (lesson.Status == LessonStatus.InReview)
            return; // already awaiting review

        SetStatus(lesson, LessonStatus.InReview);
        lesson.SubmittedAtUtc = DateTime.UtcNow;
        lesson.ReviewNote = null;
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SendBackAsync(Guid id, Guid churchId, string? note, CancellationToken ct = default)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        if (lesson.Status != LessonStatus.InReview)
            throw new ValidationException("Only a lesson awaiting review can be sent back.");

        SetStatus(lesson, LessonStatus.Draft);
        lesson.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var lesson = await db.Lessons
            .Include(l => l.MemoryVerse)
            .Include(l => l.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(l => l.Id == id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        if (lesson.MemoryVerse is null)
            throw new ValidationException("Cannot publish a lesson without a memory verse.");

        if (lesson.Quiz is null || lesson.Quiz.Questions.Count == 0)
            throw new ValidationException("Cannot publish a lesson without at least one quiz question.");

        if (lesson.Quiz.Questions.Any(q => q.Options.Count == 0 || !q.Options.Any(o => o.IsCorrect)))
            throw new ValidationException("Every quiz question must have at least one correct answer.");

        SetStatus(lesson, LessonStatus.Published);
        lesson.ReviewNote = null;
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");
        SetStatus(lesson, LessonStatus.Draft);
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        // "In use" = a child recorded progress on it, or attendance was taken against it (both are
        // Restrict FKs / real history). Its own memory verse, quiz and class-group assignments cascade.
        if (await db.ChildProgresses.AnyAsync(p => p.LessonId == id, ct))
            throw new ValidationException("Children already have progress on this lesson, so it can’t be deleted. Unpublish it instead.");
        if (await db.Attendances.AnyAsync(a => a.LessonId == id, ct))
            throw new ValidationException("This lesson has attendance recorded against it and can’t be deleted. Unpublish it instead.");

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> SeedDefaultLibraryAsync(Guid churchId, CancellationToken ct = default)
    {
        if (await db.Lessons.AnyAsync(l => l.ChurchId == churchId, ct))
            return 0; // don't duplicate — the church already has lessons

        var lessons = DefaultLessons.ForChurch(churchId).ToList();
        db.Lessons.AddRange(lessons);
        await db.SaveChangesAsync(ct);
        return lessons.Count;
    }

    public async Task AssignToClassGroupAsync(Guid lessonId, Guid churchId, Guid classGroupId, CancellationToken ct = default)
    {
        var lessonExists = await db.Lessons.AsNoTracking().AnyAsync(l => l.Id == lessonId && l.ChurchId == churchId, ct);
        if (!lessonExists)
            throw new ValidationException("Lesson not found.");

        var classGroupExists = await db.ClassGroups.AsNoTracking().AnyAsync(c => c.Id == classGroupId && c.ChurchId == churchId, ct);
        if (!classGroupExists)
            throw new ValidationException("Class group not found.");

        var alreadyAssigned = await db.LessonClassGroups.AsNoTracking()
            .AnyAsync(a => a.LessonId == lessonId && a.ClassGroupId == classGroupId, ct);
        if (alreadyAssigned)
            return;

        // Append to the end of this group's teaching order.
        var maxOrder = await db.LessonClassGroups.AsNoTracking()
            .Where(a => a.ClassGroupId == classGroupId)
            .Select(a => (int?)a.SortOrder).MaxAsync(ct) ?? -1;

        db.LessonClassGroups.Add(new LessonClassGroup { LessonId = lessonId, ClassGroupId = classGroupId, SortOrder = maxOrder + 1 });
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderLessonsAsync(Guid classGroupId, Guid churchId, IReadOnlyList<Guid> orderedLessonIds, CancellationToken ct = default)
    {
        var groupOk = await db.ClassGroups.AsNoTracking().AnyAsync(c => c.Id == classGroupId && c.ChurchId == churchId, ct);
        if (!groupOk)
            throw new ValidationException("Class group not found.");

        // Teachers may only reorder classes they're assigned to; admins (scope == null) any.
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null && !scope.Contains(classGroupId))
            throw new ValidationException("You can only reorder classes you're assigned to.");

        var assignments = await db.LessonClassGroups
            .Include(a => a.Lesson)
            .Where(a => a.ClassGroupId == classGroupId && a.Lesson.ChurchId == churchId)
            .ToListAsync(ct);

        // Apply the caller's order; anything not listed keeps a stable spot after the ordered ones.
        var index = orderedLessonIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        foreach (var a in assignments)
            a.SortOrder = index.TryGetValue(a.LessonId, out var i) ? i : orderedLessonIds.Count;

        await db.SaveChangesAsync(ct);
    }

    public async Task UnassignFromClassGroupAsync(Guid lessonId, Guid churchId, Guid classGroupId, CancellationToken ct = default)
    {
        var assignment = await db.LessonClassGroups
            .Include(a => a.Lesson)
            .FirstOrDefaultAsync(a => a.LessonId == lessonId && a.ClassGroupId == classGroupId && a.Lesson.ChurchId == churchId, ct);

        if (assignment is not null)
        {
            db.LessonClassGroups.Remove(assignment);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<Guid> AddOrUpdateQuestionAsync(Guid lessonId, Guid churchId, QuizQuestionFormModel model, CancellationToken ct = default)
    {
        if (model.Options.Count == 0 || !model.Options.Any(o => o.IsCorrect))
            throw new ValidationException("At least one correct answer must be set for the question.");

        var lesson = await db.Lessons
            .Include(l => l.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.ChurchId == churchId, ct)
            ?? throw new ValidationException("Lesson not found.");

        EnsureCanAuthor(lesson);

        if (lesson.Quiz is null)
        {
            var newQuiz = new Quiz { LessonId = lesson.Id, Title = $"{lesson.Title} Quiz" };
            db.Quizzes.Add(newQuiz);
            lesson.Quiz = newQuiz;
        }

        var question = model.Id is null
            ? null
            : lesson.Quiz.Questions.FirstOrDefault(q => q.Id == model.Id);

        if (question is null)
        {
            // Must go through db.QuizQuestions.Add (not just the Questions collection) —
            // QuizQuestion.Id is a client-generated Guid set by the property initializer,
            // so when a new instance is only reachable via a navigation on an
            // already-tracked Lesson, EF Core's change detection can't tell it apart from
            // an existing row and marks it Modified instead of Added, producing an UPDATE
            // that matches zero rows and throws DbUpdateConcurrencyException.
            question = new QuizQuestion { QuizId = lesson.Quiz.Id };
            db.QuizQuestions.Add(question);
        }
        else
        {
            db.QuizOptions.RemoveRange(question.Options);
            question.Options.Clear();
        }

        question.QuestionText = model.QuestionText.Trim();
        question.QuestionType = model.QuestionType;
        question.Points = model.Points;

        var newOptions = model.Options
            .Select(o => new QuizOption { QuizQuestionId = question.Id, OptionText = o.OptionText.Trim(), IsCorrect = o.IsCorrect })
            .ToList();
        db.QuizOptions.AddRange(newOptions);
        question.Options = newOptions;

        OnContentEdited(lesson);
        await db.SaveChangesAsync(ct);
        return question.Id;
    }

    public async Task RemoveQuestionAsync(Guid lessonId, Guid churchId, Guid questionId, CancellationToken ct = default)
    {
        var question = await db.QuizQuestions
            .Include(q => q.Quiz).ThenInclude(q => q!.Lesson)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.Quiz!.LessonId == lessonId && q.Quiz.Lesson.ChurchId == churchId, ct)
            ?? throw new ValidationException("Question not found.");

        EnsureCanAuthor(question.Quiz!.Lesson);

        db.QuizQuestions.Remove(question);
        OnContentEdited(question.Quiz.Lesson);
        await db.SaveChangesAsync(ct);
    }

    private static LessonDetailDto ToDetailDto(Lesson l, DateTime? lastCompletedAtUtc) => new(
        l.Id, l.ChurchId, l.Title, l.BibleReference, l.Theme, l.AgeGroup, l.StoryContent,
        l.LearningObjective, l.Activity, l.Prayer, l.IsPublished,
        l.Status, l.AuthorUserId, l.AuthorName, l.ReviewNote,
        l.MemoryVerse is null ? null : new MemoryVerseDto(l.MemoryVerse.VerseText, l.MemoryVerse.BibleReference, l.MemoryVerse.ShortExplanation),
        l.Quiz is null ? null : new QuizDto(l.Quiz.Id, l.Quiz.Title, l.Quiz.Description,
            l.Quiz.Questions.Select(q => new QuizQuestionDto(q.Id, q.QuestionText, q.QuestionType, q.Points,
                q.Options.Select(o => new QuizOptionDto(o.Id, o.OptionText, o.IsCorrect)).ToList())).ToList()),
        l.AssignedClassGroups.Select(a => a.ClassGroupId).ToList(),
        lastCompletedAtUtc);
}
