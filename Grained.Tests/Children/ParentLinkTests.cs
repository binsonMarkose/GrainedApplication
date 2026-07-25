using Grained.Application.Children;
using Grained.Application.Common.Services;
using Grained.Domain.Common;
using Grained.Domain.Entities;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grained.Tests.Children;

// Adding a child whose parent email matches an existing account (e.g. a teacher) links the child and
// promotes that account to Parent — so the person can switch to the Parent view. The admin confirms
// in the UI first (backed by LookupParentAsync).
public class ParentLinkTests
{
    private static async Task<(ApplicationDbContext db, UserManager<ApplicationUser> um, Church church, ClassGroup group)> SetupAsync()
    {
        var db = TestDbContextFactory.Create();
        db.Roles.AddRange(
            new IdentityRole<Guid>(Roles.Teacher) { NormalizedName = Roles.Teacher.ToUpperInvariant() },
            new IdentityRole<Guid>(Roles.Parent) { NormalizedName = Roles.Parent.ToUpperInvariant() });
        var church = new Church { Name = "Peniel", Email = "p@x.org" };
        db.Churches.Add(church);
        var group = new ClassGroup { Church = church, Name = "Nursery", MinAge = 3, MaxAge = 5 };
        db.ClassGroups.Add(group);
        await db.SaveChangesAsync();
        return (db, TestUserManager.Create(db), church, group);
    }

    private static async Task<ApplicationUser> CreateTeacherAsync(UserManager<ApplicationUser> um, Guid churchId, string email)
    {
        var teacher = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Binson", ChurchId = churchId };
        await um.CreateAsync(teacher, "Passw0rd!");
        await um.AddToRoleAsync(teacher, Roles.Teacher);
        return teacher;
    }

    private static ChildService Children(ApplicationDbContext db, UserManager<ApplicationUser> um) =>
        new(db, new TeacherScope(db, new FakeCurrentUserService()), um);

    private static ChildFormModel ChildForm(Guid groupId, string parentEmail, string first = "Marko") => new()
    {
        FirstName = first,
        LastName = "Binson",
        DateOfBirth = new DateOnly(2020, 1, 1),
        ClassGroupId = groupId,
        ParentName = "Binson",
        ParentEmail = parentEmail,
    };

    [Fact]
    public async Task Creating_a_child_with_a_teachers_email_links_and_promotes_to_parent()
    {
        var (db, um, church, group) = await SetupAsync();
        var teacher = await CreateTeacherAsync(um, church.Id, "binsboom@gmail.com");

        var childService = Children(db, um);
        var childId = await childService.CreateAsync(church.Id, ChildForm(group.Id, "binsboom@gmail.com"));

        var child = await db.Children.FirstAsync(c => c.Id == childId);
        Assert.Equal(teacher.Id, child.ParentUserId);
        Assert.True(await um.IsInRoleAsync(teacher, Roles.Parent));
        Assert.True(await um.IsInRoleAsync(teacher, Roles.Teacher)); // keeps their staff role
    }

    [Fact]
    public async Task Creating_a_second_child_links_all_siblings()
    {
        var (db, um, church, group) = await SetupAsync();
        await CreateTeacherAsync(um, church.Id, "binsboom@gmail.com");
        var childService = Children(db, um);

        var firstId = await childService.CreateAsync(church.Id, ChildForm(group.Id, "binsboom@gmail.com", "Marko"));
        var secondId = await childService.CreateAsync(church.Id, ChildForm(group.Id, "binsboom@gmail.com", "Johan"));

        var first = await db.Children.FirstAsync(c => c.Id == firstId);
        var second = await db.Children.FirstAsync(c => c.Id == secondId);
        Assert.NotNull(first.ParentUserId);
        Assert.Equal(first.ParentUserId, second.ParentUserId);
    }

    [Fact]
    public async Task LookupParent_flags_an_existing_staff_account()
    {
        var (db, um, church, group) = await SetupAsync();
        _ = group;
        await CreateTeacherAsync(um, church.Id, "binsboom@gmail.com");

        var result = await Children(db, um).LookupParentAsync(church.Id, "binsboom@gmail.com");

        Assert.True(result.Exists);
        Assert.True(result.IsStaff);
        Assert.False(result.AlreadyParent);
        Assert.Equal("Binson", result.Name);
    }

    [Fact]
    public async Task LookupParent_returns_not_found_for_an_unknown_email()
    {
        var (db, um, church, _) = await SetupAsync();

        var result = await Children(db, um).LookupParentAsync(church.Id, "nobody@example.com");

        Assert.False(result.Exists);
    }
}
