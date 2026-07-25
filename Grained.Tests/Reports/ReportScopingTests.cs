using Grained.Application.Common.Services;
using Grained.Application.Growth;
using Grained.Application.Reports;
using Grained.Domain.Entities;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;

namespace Grained.Tests.Reports;

public class ReportScopingTests
{
    // Builds a church with two classes (A, B) — a child in each — and a teacher assigned to class A only.
    private static async Task<(ApplicationDbContext db, Guid churchId, Guid teacherUserId, Guid childInAId, Guid childInBId)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Grace", Email = "grace@church.org" };
        var classA = new ClassGroup { Church = church, Name = "Class A", MinAge = 5, MaxAge = 7 };
        var classB = new ClassGroup { Church = church, Name = "Class B", MinAge = 8, MaxAge = 10 };
        var alice = new Child { Church = church, ClassGroup = classA, FirstName = "Alice", LastName = "A", ParentEmail = "a@x.org", DateOfBirth = new DateOnly(2018, 1, 1) };
        var bob = new Child { Church = church, ClassGroup = classB, FirstName = "Bob", LastName = "B", ParentEmail = "b@x.org", DateOfBirth = new DateOnly(2016, 1, 1) };

        var teacherUser = new ApplicationUser { UserName = "t@x.org", Email = "t@x.org", FullName = "Teacher T", Church = church };
        var profile = new TeacherProfile { ApplicationUser = teacherUser, Church = church };
        var assignment = new TeacherClassGroup { TeacherProfile = profile, ClassGroup = classA };

        db.AddRange(church, classA, classB, alice, bob, teacherUser, profile, assignment);
        await db.SaveChangesAsync();

        return (db, church.Id, teacherUser.Id, alice.Id, bob.Id);
    }

    private static ReportService ReportsFor(ApplicationDbContext db, FakeCurrentUserService currentUser)
    {
        var scope = new TeacherScope(db, currentUser);
        return new ReportService(db, new GrowthService(db), scope);
    }

    [Fact]
    public async Task Teacher_ChildProgress_OnlySeesOwnClassChildren()
    {
        var (db, churchId, teacherUserId, _, _) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { UserId = teacherUserId, ChurchId = churchId, IsChurchAdmin = false, IsTeacher = true };

        var rows = await ReportsFor(db, currentUser).GetChildProgressReportAsync(churchId);

        var row = Assert.Single(rows);
        Assert.Equal("Alice A", row.ChildName);
        Assert.Equal("Class A", row.ClassGroupName);
    }

    [Fact]
    public async Task Admin_ChildProgress_SeesEveryChild()
    {
        var (db, churchId, _, _, _) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { ChurchId = churchId, IsChurchAdmin = true };

        var rows = await ReportsFor(db, currentUser).GetChildProgressReportAsync(churchId);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task Teacher_ClassProgress_OnlySeesOwnClass()
    {
        var (db, churchId, teacherUserId, _, _) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { UserId = teacherUserId, ChurchId = churchId, IsChurchAdmin = false, IsTeacher = true };

        var rows = await ReportsFor(db, currentUser).GetClassProgressReportAsync(churchId);

        var row = Assert.Single(rows);
        Assert.Equal("Class A", row.ClassGroupName);
    }

    [Fact]
    public async Task Teacher_CannotLoadBadgesForChildOutsideTheirClass()
    {
        var (db, churchId, teacherUserId, _, childInBId) = await SeedAsync();
        var currentUser = new FakeCurrentUserService { UserId = teacherUserId, ChurchId = churchId, IsChurchAdmin = false, IsTeacher = true };

        await Assert.ThrowsAsync<Grained.Application.Common.Exceptions.ValidationException>(
            () => ReportsFor(db, currentUser).GetChildBadgesAsync(childInBId, churchId));
    }
}
