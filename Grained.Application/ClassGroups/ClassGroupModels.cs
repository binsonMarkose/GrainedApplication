using System.ComponentModel.DataAnnotations;

namespace Grained.Application.ClassGroups;

public record ClassGroupDto(
    Guid Id,
    Guid ChurchId,
    string Name,
    int MinAge,
    int MaxAge,
    string? Description,
    bool IsActive,
    int ChildCount);

public class ClassGroupFormModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Class group name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Minimum age is required")]
    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120")]
    public int MinAge { get; set; }

    [Required(ErrorMessage = "Maximum age is required")]
    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120")]
    public int MaxAge { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxAge < MinAge)
        {
            yield return new ValidationResult(
                "Maximum age must be greater than or equal to minimum age",
                [nameof(MaxAge)]);
        }
    }
}
