using System.ComponentModel.DataAnnotations;
using Grained.Domain.Enums;

namespace Grained.Application.Badges;

public record BadgeDto(
    Guid Id,
    Guid ChurchId,
    string Name,
    string? Description,
    string? IconName,
    string? Criteria,
    BadgeTier Tier,
    int Points,
    bool IsActive,
    bool Repeatable);

public class BadgeFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Badge name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? IconName { get; set; }

    [MaxLength(500)]
    public string? Criteria { get; set; }

    public BadgeTier Tier { get; set; } = BadgeTier.Standard;

    // Growth points this badge is worth. When 0/unset we default by tier.
    public int Points { get; set; }

    // Can this badge be awarded to the same child more than once? Defaults align with tier in the UI
    // (Standard = repeatable, Achievement = one-time).
    public bool Repeatable { get; set; }
}
