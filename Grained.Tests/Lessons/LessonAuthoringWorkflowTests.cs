using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Services;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;

namespace Grained.Tests.Lessons;

// Covers the teacher-authoring + admin-review lifecycle added on top of the base LessonService tests.
public class LessonAuthoringWorkflowTests
{
    private static LessonFormModel ValidForm() => new()
    {
        Title = "Noah's Ark",
        BibleReference = "Genesis 6",
        AgeGroup = "Ages 5-10",
        StoryContent = "God asked Noah to build an ark...",
        MemoryVerse = new MemoryVerseFormModel { VerseText = "By faith Noah built an ark.", BibleReference = "Hebrews 11:7" }
    };

    private static QuizQuestionFormModel ValidQuestion() => new()
    {
        QuestionText = "Who built the ark?",
        QuestionType = QuestionType.SingleChoice,
        Points = 1,
        Options = [new() { OptionText = "Noah", IsCorrect = true }, new() { OptionText = "Moses", IsCorrect = false }],
    };

    private static LessonService ServiceFor(ApplicationDbContext db, FakeCurrentUserService user) =>
        new(db, new TeacherScope(db, user), user);

    private static async Task<(ApplicationDbContext db, Guid churchId, Guid classGroupId, ApplicationUser teacherUser, ApplicationUser otherTeacherUser)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Grace", Email = "g@c.org" };
        var cg = new ClassGroup { Church = church, Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };

        var teacherUser = new ApplicationUser { UserName = "t@c.org", Email = "t@c.org", FullName = "Teacher One", Church = church };
        var teacherProfile = new TeacherProfile { ApplicationUser = teacherUser, Church = church };
        var teacherAssign = new TeacherClassGroup { TeacherProfile = teacherProfile, ClassGroup = cg };

        var other = new ApplicationUser { UserName = "t2@c.org", Email = "t2@c.org", FullName = "Teacher Two", Church = church };
        var otherProfile = new TeacherProfile { ApplicationUser = other, Church = church };
        var otherAssign = new TeacherClassGroup { TeacherProfile = otherProfile, ClassGroup = cg };

        db.AddRange(church, cg, teacherUser, teacherProfile, teacherAssign, other, otherProfile, otherAssign);
        await db.SaveChangesAsync();
        return (db, church.Id, cg.Id, teacherUser, other);
    }

    private static FakeCurrentUserService AsTeacher(Guid churchId, Guid userId) =>
        new() { UserId = userId, ChurchId = churchId, IsChurchAdmin = false, IsTeacher = true };
    private static FakeCurrentUserService AsAdmin(Guid churchId) =>
        new() { ChurchId = churchId, IsChurchAdmin = true };

    [Fact]
    public async Task TeacherCreates_StartsAsDraft_WithAuthorStamped()
    {
        var (db, churchId, _, teacher, _) = await SeedAsync();
        var svc = ServiceFor(db, AsTeacher(churchId, teacher.Id));

        var id = await svc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());

        var detail = await svc.GetDetailAsync(id, churchId);
        Assert.Equal(LessonStatus.Draft, detail!.Status);
        Assert.False(detail.IsPublished);
        Assert.Equal(teacher.Id, detail.AuthorUserId);
        Assert.Equal("Teacher One", detail.AuthorName);
    }

    [Fact]
    public async Task TeacherCannotPublish_ButCanSubmit_ThenAdminPublishes()
    {
        var (db, churchId, _, teacher, _) = await SeedAsync();
        var teacherSvc = ServiceFor(db, AsTeacher(churchId, teacher.Id));
        var id = await teacherSvc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());
        await teacherSvc.AddOrUpdateQuestionAsync(id, churchId, ValidQuestion());

        await teacherSvc.SubmitForReviewAsync(id, churchId);
        Assert.Equal(LessonStatus.InReview, (await teacherSvc.GetDetailAsync(id, churchId))!.Status);

        // Admin publishes.
        var adminSvc = ServiceFor(db, AsAdmin(churchId));
        await adminSvc.PublishAsync(id, churchId);
        Assert.Equal(LessonStatus.Published, (await adminSvc.GetDetailAsync(id, churchId))!.Status);
    }

    [Fact]
    public async Task TeacherCannotEditAnotherTeachersLesson()
    {
        var (db, churchId, _, teacher, other) = await SeedAsync();
        var id = await ServiceFor(db, AsTeacher(churchId, teacher.Id)).CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());

        var otherSvc = ServiceFor(db, AsTeacher(churchId, other.Id));
        var form = ValidForm();
        form.Id = id;
        await Assert.ThrowsAsync<ValidationException>(() => otherSvc.UpdateAsync(churchId, form));
    }

    [Fact]
    public async Task TeacherEditingPublishedLesson_SendsItBackToReview()
    {
        var (db, churchId, _, teacher, _) = await SeedAsync();
        var teacherSvc = ServiceFor(db, AsTeacher(churchId, teacher.Id));
        var id = await teacherSvc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());
        await teacherSvc.AddOrUpdateQuestionAsync(id, churchId, ValidQuestion());
        await ServiceFor(db, AsAdmin(churchId)).PublishAsync(id, churchId);

        // The author tweaks the published lesson → it drops back to InReview (needs re-approval).
        var form = ValidForm();
        form.Id = id;
        form.StoryContent = "Updated story text.";
        await teacherSvc.UpdateAsync(churchId, form);

        var detail = await teacherSvc.GetDetailAsync(id, churchId);
        Assert.Equal(LessonStatus.InReview, detail!.Status);
        Assert.False(detail.IsPublished);
    }

    [Fact]
    public async Task AdminSendBack_ReturnsToDraft_WithNote()
    {
        var (db, churchId, _, teacher, _) = await SeedAsync();
        var teacherSvc = ServiceFor(db, AsTeacher(churchId, teacher.Id));
        var id = await teacherSvc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());
        await teacherSvc.SubmitForReviewAsync(id, churchId);

        await ServiceFor(db, AsAdmin(churchId)).SendBackAsync(id, churchId, "Please add an activity.");

        var detail = await teacherSvc.GetDetailAsync(id, churchId);
        Assert.Equal(LessonStatus.Draft, detail!.Status);
        Assert.Equal("Please add an activity.", detail.ReviewNote);
    }

    [Fact]
    public async Task TeacherList_ShowsOwnDrafts_ButNotOtherTeachersDrafts()
    {
        var (db, churchId, _, teacher, other) = await SeedAsync();
        var mineId = await ServiceFor(db, AsTeacher(churchId, teacher.Id)).CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());
        var theirsId = await ServiceFor(db, AsTeacher(churchId, other.Id)).CreateAsync(churchId, other.Id, other.FullName, ValidForm());

        var list = await ServiceFor(db, AsTeacher(churchId, teacher.Id)).GetForChurchAsync(churchId);

        Assert.Contains(list, l => l.Id == mineId);
        Assert.DoesNotContain(list, l => l.Id == theirsId);
    }

    [Fact]
    public async Task AdminReviewQueue_FiltersByStatus()
    {
        var (db, churchId, _, teacher, _) = await SeedAsync();
        var teacherSvc = ServiceFor(db, AsTeacher(churchId, teacher.Id));
        var submittedId = await teacherSvc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm());
        await teacherSvc.SubmitForReviewAsync(submittedId, churchId);
        await teacherSvc.CreateAsync(churchId, teacher.Id, teacher.FullName, ValidForm()); // stays Draft

        var queue = await ServiceFor(db, AsAdmin(churchId)).GetForChurchAsync(churchId, status: LessonStatus.InReview);

        var only = Assert.Single(queue);
        Assert.Equal(submittedId, only.Id);
    }
}
