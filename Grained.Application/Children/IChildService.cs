namespace Grained.Application.Children;

public interface IChildService
{
    Task<List<ChildDto>> GetForChurchAsync(Guid churchId, ChildFilter filter, CancellationToken ct = default);
    Task<ChildDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, ChildFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, ChildFormModel model, CancellationToken ct = default);
    Task AssignClassGroupAsync(Guid id, Guid churchId, Guid classGroupId, CancellationToken ct = default);

    // Does this parent email already match an account in the church? Lets the UI warn before a save
    // links (and grants Parent access to) an existing account.
    Task<ParentLookupResult> LookupParentAsync(Guid churchId, string email, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);

    // Permanently removes the child and their badges, progress and attendance. The parent account is
    // left intact.
    Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default);

    // Ensures a parent account exists for this child's parent email, links the child (and any
    // siblings) to it, and returns a login code to show the admin. If the email already belongs to
    // an account (e.g. a teacher who is also a parent), no code is issued — the children are simply
    // linked and the parent uses their existing login (Result.LinkedExistingAccount = true).
    Task<ParentCodeResult> CreateOrResetParentCodeAsync(Guid childId, Guid churchId, CancellationToken ct = default);
}
