using Microsoft.AspNetCore.Identity;

namespace Grained.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    // Null for SuperAdmin, who is not scoped to a single church.
    public Guid? ChurchId { get; set; }
    public Church? Church { get; set; }

    public bool IsActive { get; set; } = true;

    // Set when an admin provisions the account with a temporary login code; the user is forced to
    // choose their own password on first sign-in, then it's cleared.
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public TeacherProfile? TeacherProfile { get; set; }
}
