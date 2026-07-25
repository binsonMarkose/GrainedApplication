using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Grained.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Church> Churches => Set<Church>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<TeacherClassGroup> TeacherClassGroups => Set<TeacherClassGroup>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonClassGroup> LessonClassGroups => Set<LessonClassGroup>();
    public DbSet<MemoryVerse> MemoryVerses => Set<MemoryVerse>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<ChildBadge> ChildBadges => Set<ChildBadge>();
    public DbSet<GrowthSeason> GrowthSeasons => Set<GrowthSeason>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventTicketType> EventTicketTypes => Set<EventTicketType>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<EventRegistrationLine> EventRegistrationLines => Set<EventRegistrationLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<StoredImage> StoredImages => Set<StoredImage>();
    public DbSet<ChildProgress> ChildProgresses => Set<ChildProgress>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementReceipt> AnnouncementReceipts => Set<AnnouncementReceipt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Church>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Slug).HasMaxLength(120);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Invitation>(e =>
        {
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Invitations)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Users)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeacherProfile>(e =>
        {
            e.HasIndex(x => x.ApplicationUserId).IsUnique();
            e.HasOne(x => x.ApplicationUser)
                .WithOne(u => u.TeacherProfile)
                .HasForeignKey<TeacherProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Church)
                .WithMany()
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeacherClassGroup>(e =>
        {
            e.HasIndex(x => new { x.TeacherProfileId, x.ClassGroupId }).IsUnique();
            e.HasOne(x => x.TeacherProfile)
                .WithMany(t => t.AssignedClassGroups)
                .HasForeignKey(x => x.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ClassGroup)
                .WithMany(c => c.AssignedTeachers)
                .HasForeignKey(x => x.ClassGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClassGroup>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Church)
                .WithMany(c => c.ClassGroups)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Child>(e =>
        {
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.ParentEmail).IsRequired().HasMaxLength(256);
            e.Property(x => x.AvatarId).HasMaxLength(40);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Children)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ClassGroup)
                .WithMany(c => c.Children)
                .HasForeignKey(x => x.ClassGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ParentUser)
                .WithMany()
                .HasForeignKey(x => x.ParentUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.ParentUserId);
        });

        builder.Entity<Lesson>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.BibleReference).IsRequired().HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.AuthorName).HasMaxLength(200);
            e.Property(x => x.ReviewNote).HasMaxLength(1000);
            e.HasIndex(x => new { x.ChurchId, x.Status });
            e.HasIndex(x => x.AuthorUserId);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Lessons)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LessonClassGroup>(e =>
        {
            e.HasIndex(x => new { x.LessonId, x.ClassGroupId }).IsUnique();
            e.HasOne(x => x.Lesson)
                .WithMany(l => l.AssignedClassGroups)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ClassGroup)
                .WithMany(c => c.AssignedLessons)
                .HasForeignKey(x => x.ClassGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MemoryVerse>(e =>
        {
            e.HasIndex(x => x.LessonId).IsUnique();
            e.Property(x => x.VerseText).IsRequired();
            e.Property(x => x.BibleReference).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Lesson)
                .WithOne(l => l.MemoryVerse)
                .HasForeignKey<MemoryVerse>(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Quiz>(e =>
        {
            e.HasIndex(x => x.LessonId).IsUnique();
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Lesson)
                .WithOne(l => l.Quiz)
                .HasForeignKey<Quiz>(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizQuestion>(e =>
        {
            e.Property(x => x.QuestionText).IsRequired();
            e.HasOne(x => x.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizOption>(e =>
        {
            e.Property(x => x.OptionText).IsRequired();
            e.HasOne(x => x.QuizQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(x => x.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Badge>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Tier).HasConversion<int>();
            e.HasOne(x => x.Church)
                .WithMany(c => c.Badges)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GrowthSeason>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(60);
            e.HasIndex(x => new { x.ChurchId, x.StartsOnUtc });
            e.HasOne(x => x.Church)
                .WithMany()
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChildBadge>(e =>
        {
            // Not unique: repeatable badges (effort/character) can be awarded to a child many times;
            // one-time enforcement for milestone badges lives in the service (Badge.Repeatable).
            e.HasIndex(x => new { x.ChildId, x.BadgeId });
            e.HasOne(x => x.Child)
                .WithMany(c => c.ChildBadges)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Badge)
                .WithMany(b => b.ChildBadges)
                .HasForeignKey(x => x.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Event>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Events)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventTicketType>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne(x => x.Event)
                .WithMany(ev => ev.TicketTypes)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.Property(x => x.Provider).IsRequired().HasMaxLength(30);
            e.Property(x => x.ProviderReference).HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PayerName).HasMaxLength(200);
            e.Property(x => x.PayerEmail).HasMaxLength(256);
            e.HasOne(x => x.Church)
                .WithMany()
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventRegistration>(e =>
        {
            e.Property(x => x.PurchaserName).IsRequired().HasMaxLength(200);
            e.Property(x => x.PurchaserEmail).IsRequired().HasMaxLength(256);
            e.Property(x => x.PurchaserPhone).HasMaxLength(40);
            e.Property(x => x.TshirtSize).HasMaxLength(20);
            e.Property(x => x.Total).HasPrecision(10, 2);
            e.HasOne(x => x.Event)
                .WithMany()
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Lines)
                .WithOne(l => l.EventRegistration)
                .HasForeignKey(l => l.EventRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventRegistrationLine>(e =>
        {
            e.Property(x => x.TicketTypeName).IsRequired().HasMaxLength(100);
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
        });

        builder.Entity<StoredImage>(e =>
        {
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Campaign>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.TargetAmount).HasPrecision(10, 2);
            e.HasOne(x => x.Church)
                .WithMany(c => c.Campaigns)
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Donation>(e =>
        {
            e.Property(x => x.DonorName).IsRequired().HasMaxLength(200);
            e.Property(x => x.DonorEmail).IsRequired().HasMaxLength(256);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.HasOne(x => x.Campaign)
                .WithMany(c => c.Donations)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ChildProgress>(e =>
        {
            e.HasIndex(x => new { x.ChildId, x.LessonId }).IsUnique();
            e.HasOne(x => x.Child)
                .WithMany(c => c.ChildProgresses)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lesson)
                .WithMany(l => l.ChildProgresses)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attendance>(e =>
        {
            e.HasOne(x => x.Child)
                .WithMany(c => c.Attendances)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ClassGroup)
                .WithMany(c => c.Attendances)
                .HasForeignKey(x => x.ClassGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Lesson)
                .WithMany(l => l.Attendances)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Announcement>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(150);
            e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            e.Property(x => x.Audience).HasConversion<int>();
            e.Property(x => x.CreatedByName).HasMaxLength(200);
            e.HasIndex(x => x.ChurchId);
            e.HasOne(x => x.Church)
                .WithMany()
                .HasForeignKey(x => x.ChurchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AnnouncementReceipt>(e =>
        {
            e.HasIndex(x => new { x.AnnouncementId, x.UserId }).IsUnique();
            e.HasOne(x => x.Announcement)
                .WithMany(a => a.Receipts)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
