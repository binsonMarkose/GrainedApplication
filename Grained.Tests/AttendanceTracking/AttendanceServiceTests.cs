using Grained.Application.AttendanceTracking;
using Grained.Domain.Entities;
using Grained.Tests.Common;

namespace Grained.Tests.AttendanceTracking;

public class AttendanceServiceTests
{
    private static async Task<(AttendanceService Service, Guid ChurchId, Guid ClassGroupId, Guid ChildId)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Test Church", Email = "test@church.org" };
        var classGroup = new ClassGroup { Church = church, Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };
        var child = new Child
        {
            Church = church,
            ClassGroup = classGroup,
            FirstName = "Timmy",
            LastName = "Tester",
            DateOfBirth = new DateOnly(2018, 1, 1),
            ParentName = "Parent",
            ParentEmail = "parent@example.com"
        };
        db.Churches.Add(church);
        db.ClassGroups.Add(classGroup);
        db.Children.Add(child);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { ChurchId = church.Id, IsChurchAdmin = true };
        return (new AttendanceService(db, currentUser), church.Id, classGroup.Id, child.Id);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewAttendanceRecord()
    {
        var (service, churchId, classGroupId, childId) = await SeedAsync();
        var date = new DateOnly(2026, 1, 4);

        await service.SaveAsync(churchId, new AttendanceSaveModel
        {
            ClassGroupId = classGroupId,
            AttendanceDate = date,
            Entries = [new AttendanceEntryFormModel { ChildId = childId, IsPresent = true, Notes = "On time" }]
        });

        var roster = await service.GetRosterAsync(churchId, classGroupId, date);
        var entry = Assert.Single(roster);
        Assert.True(entry.IsPresent);
        Assert.Equal("On time", entry.Notes);
    }

    [Fact]
    public async Task SaveAsync_CalledTwiceForSameDate_UpdatesExistingRecordInsteadOfDuplicating()
    {
        var (service, churchId, classGroupId, childId) = await SeedAsync();
        var date = new DateOnly(2026, 1, 4);

        await service.SaveAsync(churchId, new AttendanceSaveModel
        {
            ClassGroupId = classGroupId,
            AttendanceDate = date,
            Entries = [new AttendanceEntryFormModel { ChildId = childId, IsPresent = false }]
        });

        await service.SaveAsync(churchId, new AttendanceSaveModel
        {
            ClassGroupId = classGroupId,
            AttendanceDate = date,
            Entries = [new AttendanceEntryFormModel { ChildId = childId, IsPresent = true }]
        });

        var roster = await service.GetRosterAsync(churchId, classGroupId, date);
        var entry = Assert.Single(roster);
        Assert.True(entry.IsPresent);
    }
}
