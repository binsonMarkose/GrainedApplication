using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Church> Churches { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<ClassGroup> ClassGroups { get; }
    DbSet<Child> Children { get; }
    DbSet<TeacherProfile> TeacherProfiles { get; }
    DbSet<TeacherClassGroup> TeacherClassGroups { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonClassGroup> LessonClassGroups { get; }
    DbSet<MemoryVerse> MemoryVerses { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizOption> QuizOptions { get; }
    DbSet<Badge> Badges { get; }
    DbSet<ChildBadge> ChildBadges { get; }
    DbSet<GrowthSeason> GrowthSeasons { get; }
    DbSet<Event> Events { get; }
    DbSet<EventTicketType> EventTicketTypes { get; }
    DbSet<EventRegistration> EventRegistrations { get; }
    DbSet<EventRegistrationLine> EventRegistrationLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<Donation> Donations { get; }
    DbSet<StoredImage> StoredImages { get; }
    DbSet<ChildProgress> ChildProgresses { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<AnnouncementReceipt> AnnouncementReceipts { get; }
    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
