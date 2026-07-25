namespace Grained.Application.Teachers;

public interface ITeacherService
{
    Task<List<TeacherDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default);
    Task<TeacherDto?> GetByIdAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default);

    // Returns the teacher profile id and the generated temporary password for the new account.
    Task<(Guid TeacherProfileId, string TemporaryPassword)> CreateAsync(Guid churchId, TeacherFormModel model, CancellationToken ct = default);

    Task UpdateAsync(Guid churchId, TeacherFormModel model, CancellationToken ct = default);
    Task SetActiveAsync(Guid teacherProfileId, Guid churchId, bool isActive, CancellationToken ct = default);

    // Removes the teacher. If the account is solely a teacher it's deleted outright (login removed);
    // if it's also a ChurchAdmin or a linked parent, only the teacher role + assignments are dropped.
    Task DeleteAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default);

    // Generates a fresh login code, resets the teacher's password to it, and returns it.
    // The original code cannot be retrieved (only its hash is stored), so this is how an admin
    // obtains a working code again.
    Task<string> ResetLoginCodeAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default);
}
