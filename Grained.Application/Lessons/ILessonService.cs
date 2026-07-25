using Grained.Domain.Enums;

namespace Grained.Application.Lessons;

public interface ILessonService
{
    Task<List<LessonListItemDto>> GetForChurchAsync(Guid churchId, bool? publishedOnly = null, LessonStatus? status = null, Guid? classGroupId = null, CancellationToken ct = default);

    // Sets the teaching order of a group's lessons to the given lesson-id sequence (ChurchAdmin).
    Task ReorderLessonsAsync(Guid classGroupId, Guid churchId, IReadOnlyList<Guid> orderedLessonIds, CancellationToken ct = default);
    Task<LessonDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default);

    // Author is stamped from the current user (a teacher or an admin).
    Task<Guid> CreateAsync(Guid churchId, Guid authorUserId, string authorName, LessonFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, LessonFormModel model, CancellationToken ct = default);

    // Teacher submits a draft for admin review.
    Task SubmitForReviewAsync(Guid id, Guid churchId, CancellationToken ct = default);
    // Admin sends a submitted lesson back to Draft with a note.
    Task SendBackAsync(Guid id, Guid churchId, string? note, CancellationToken ct = default);

    Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default);

    // Permanently removes a lesson (and its memory verse, quiz and class-group assignments). Throws
    // if it's in use — any child progress or attendance recorded against it. Unpublish instead then.
    Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default);

    // Seeds the default Nursery starter library into a church that has no lessons yet. Idempotent —
    // returns the number of lessons added (0 if the church already has lessons).
    Task<int> SeedDefaultLibraryAsync(Guid churchId, CancellationToken ct = default);

    Task AssignToClassGroupAsync(Guid lessonId, Guid churchId, Guid classGroupId, CancellationToken ct = default);
    Task UnassignFromClassGroupAsync(Guid lessonId, Guid churchId, Guid classGroupId, CancellationToken ct = default);

    Task<Guid> AddOrUpdateQuestionAsync(Guid lessonId, Guid churchId, QuizQuestionFormModel model, CancellationToken ct = default);
    Task RemoveQuestionAsync(Guid lessonId, Guid churchId, Guid questionId, CancellationToken ct = default);
}
