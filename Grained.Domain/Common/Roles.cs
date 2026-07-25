namespace Grained.Domain.Common;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string ChurchAdmin = "ChurchAdmin";
    public const string Teacher = "Teacher";
    public const string Parent = "Parent";

    public static readonly string[] All = [SuperAdmin, ChurchAdmin, Teacher, Parent];
}
