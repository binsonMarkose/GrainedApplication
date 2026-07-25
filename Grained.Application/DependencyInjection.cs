using Grained.Application.Announcements;
using Grained.Application.AttendanceTracking;
using Grained.Application.Badges;
using Grained.Application.Children;
using Grained.Application.ClassGroups;
using Grained.Application.Churches;
using Grained.Application.Common.Interfaces;
using Grained.Application.Dashboard;
using Grained.Application.Events;
using Grained.Application.Fundraising;
using Grained.Application.Lessons;
using Grained.Application.Onboarding;
using Grained.Application.Progress;
using Grained.Application.Public;
using Grained.Application.Reports;
using Grained.Application.Teachers;
using Grained.Application.TeacherWorkspace;
using Microsoft.Extensions.DependencyInjection;

namespace Grained.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Transient, not Scoped: in Blazor Server the DI scope lives for the whole circuit
        // (the user's session), so a Scoped service here would be constructed once and keep
        // its injected IApplicationDbContext (also Transient — see Infrastructure's
        // DependencyInjection) for every page the user ever visits. Transient means each
        // page component gets a fresh service instance, and therefore a fresh DbContext.
        services.AddTransient<Common.Interfaces.ITeacherScope, Common.Services.TeacherScope>();
        services.AddTransient<IChurchService, ChurchService>();
        services.AddTransient<IClassGroupService, ClassGroupService>();
        services.AddTransient<IChildService, ChildService>();
        services.AddTransient<ITeacherService, TeacherService>();
        services.AddTransient<ITeacherWorkspaceService, TeacherWorkspaceService>();
        services.AddTransient<ParentWorkspace.IParentWorkspaceService, ParentWorkspace.ParentWorkspaceService>();
        services.AddTransient<Growth.IGrowthService, Growth.GrowthService>();
        services.AddTransient<ILessonService, LessonService>();
        services.AddTransient<IEventService, EventService>();
        services.AddTransient<IBadgeService, BadgeService>();
        services.AddTransient<IAnnouncementService, AnnouncementService>();
        services.AddTransient<IAttendanceService, AttendanceService>();
        services.AddTransient<IChildProgressService, ChildProgressService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddTransient<IChurchOnboardingService, ChurchOnboardingService>();
        services.AddTransient<ICampaignService, CampaignService>();
        services.AddTransient<IPublicEventService, PublicEventService>();
        services.AddTransient<IPublicCampaignService, PublicCampaignService>();

        return services;
    }
}
