using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Teachers;

public record TeacherDto(
    Guid TeacherProfileId,
    Guid ApplicationUserId,
    string FullName,
    string Email,
    bool IsActive,
    List<Guid> AssignedClassGroupIds,
    List<string> AssignedClassGroupNames);

public class TeacherFormModel
{
    public Guid? TeacherProfileId { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    public List<Guid> AssignedClassGroupIds { get; set; } = [];
}
