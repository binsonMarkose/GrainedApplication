using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Churches;

public record ChurchDto(
    Guid Id,
    string Name,
    string? Address,
    string Email,
    string? Phone,
    bool IsActive,
    string Status,
    DateTime CreatedAtUtc);

public class ChurchFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Church name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    public string? Phone { get; set; }
}
