namespace Grained.Domain.Enums;

// The role an invitation grants. (A full Membership join table is a later ticket; today the
// accepted ChurchAdmin is recorded via ApplicationUser.ChurchId + the ChurchAdmin identity role.)
public enum MembershipRole
{
    ChurchAdmin,
    Teacher
}
