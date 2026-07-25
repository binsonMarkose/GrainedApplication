using Grained.Application.ClassGroups;
using Grained.Application.Children;
using Grained.Application.Common.Services;
using Grained.Domain.Entities;
using Grained.Tests.Common;

namespace Grained.Tests.DataIsolation;

public class ChurchDataIsolationTests
{
    [Fact]
    public async Task ClassGroupService_GetAllForChurchAsync_OnlyReturnsOwnChurchsGroups()
    {
        var db = TestDbContextFactory.Create();
        var churchA = new Church { Name = "Church A", Email = "a@church.org" };
        var churchB = new Church { Name = "Church B", Email = "b@church.org" };
        db.Churches.AddRange(churchA, churchB);
        await db.SaveChangesAsync();

        var service = new ClassGroupService(db, new TeacherScope(db, new FakeCurrentUserService()));
        await service.CreateAsync(churchA.Id, new ClassGroupFormModel { Name = "A's Class", MinAge = 5, MaxAge = 10 });
        await service.CreateAsync(churchB.Id, new ClassGroupFormModel { Name = "B's Class", MinAge = 5, MaxAge = 10 });

        var churchAGroups = await service.GetAllForChurchAsync(churchA.Id);

        var group = Assert.Single(churchAGroups);
        Assert.Equal("A's Class", group.Name);
    }

    [Fact]
    public async Task ChildService_GetByIdAsync_ReturnsNull_WhenChildBelongsToDifferentChurch()
    {
        var db = TestDbContextFactory.Create();
        var churchA = new Church { Name = "Church A", Email = "a@church.org" };
        var churchB = new Church { Name = "Church B", Email = "b@church.org" };
        db.Churches.AddRange(churchA, churchB);
        var classGroup = new ClassGroup { Church = churchA, Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };
        db.ClassGroups.Add(classGroup);
        await db.SaveChangesAsync();

        var childService = new ChildService(db, new TeacherScope(db, new FakeCurrentUserService()), TestUserManager.Create(db));
        var childId = await childService.CreateAsync(churchA.Id, new ChildFormModel
        {
            FirstName = "Alice",
            LastName = "Smith",
            DateOfBirth = new DateOnly(2018, 1, 1),
            ClassGroupId = classGroup.Id,
            ParentName = "Parent Smith",
            ParentEmail = "parent@example.com"
        });

        // A ChurchAdmin from Church B must not be able to load a child belonging to Church A.
        var result = await childService.GetByIdAsync(childId, churchB.Id);

        Assert.Null(result);
    }
}
