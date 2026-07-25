using System.ComponentModel.DataAnnotations;

namespace Grained.Application.Children;

public record ChildDto(
    Guid Id,
    Guid ChurchId,
    Guid ClassGroupId,
    string ClassGroupName,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    int Age,
    string ParentName,
    string ParentEmail,
    string? ParentPhone,
    bool IsActive);

public class ChildFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-6));

    [Required(ErrorMessage = "A class group must be assigned")]
    public Guid? ClassGroupId { get; set; }

    [Required(ErrorMessage = "Parent/guardian name is required")]
    [MaxLength(200)]
    public string ParentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parent/guardian email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string ParentEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    public string? ParentPhone { get; set; }
}

// Result of generating a parent login code.
// Code is null when the parent already has a Grained account (e.g. they're also a teacher/admin):
// in that case we just link the children and they keep their existing login.
public record ParentCodeResult(string? Code, bool LinkedExistingAccount, string ParentEmail);

// Does this parent email already match an account in the church? Used to warn the admin before a
// child save links (and grants Parent access to) an existing account — e.g. a teacher.
public record ParentLookupResult(bool Exists, string? Name, bool IsStaff, bool AlreadyParent);

public class ChildFilter
{
    public Guid? ClassGroupId { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public bool? IsActive { get; set; } = true;
}
