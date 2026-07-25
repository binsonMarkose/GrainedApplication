using Grained.Application.Badges;
using Grained.Domain.Common;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grained.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        bool seedSampleData = false)
    {
        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        if (await userManager.FindByEmailAsync("superadmin@grained.org") is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = "superadmin@grained.org",
                Email = "superadmin@grained.org",
                EmailConfirmed = true,
                FullName = "Super Admin"
            };
            await userManager.CreateAsync(superAdmin, "ChangeMe123!");
            await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        }

        // Demo/sample data is opt-in (SeedSampleData). Roles + SuperAdmin above are always ensured;
        // everything below only runs when explicitly enabled and the DB has no church yet.
        if (!seedSampleData || await db.Churches.AnyAsync())
            return;

        var church = new Church
        {
            Name = "Grace Community Church",
            Slug = "grace-community-church",
            Address = "123 Main Street",
            Email = "office@gracecommunity.org",
            Phone = "555-0100"
        };
        db.Churches.Add(church);

        var churchAdmin = new ApplicationUser
        {
            UserName = "admin@gracecommunity.org",
            Email = "admin@gracecommunity.org",
            EmailConfirmed = true,
            FullName = "Grace Church Admin",
            ChurchId = church.Id
        };
        await userManager.CreateAsync(churchAdmin, "ChangeMe123!");
        await userManager.AddToRoleAsync(churchAdmin, Roles.ChurchAdmin);

        var classGroups = new[]
        {
            new ClassGroup { ChurchId = church.Id, Name = "Under 5", MinAge = 0, MaxAge = 4, Description = "Our youngest little disciples" },
            new ClassGroup { ChurchId = church.Id, Name = "Ages 5-10", MinAge = 5, MaxAge = 10, Description = "Building strong foundations" },
            new ClassGroup { ChurchId = church.Id, Name = "Ages 10-14", MinAge = 10, MaxAge = 14, Description = "Growing in faith" },
            new ClassGroup { ChurchId = church.Id, Name = "Ages 14-18", MinAge = 14, MaxAge = 18, Description = "Preparing young leaders" }
        };
        db.ClassGroups.AddRange(classGroups);

        db.Badges.AddRange(DefaultBadges.ForChurch(church.Id));

        var ages5To10 = classGroups[1];

        var creation = new Lesson
        {
            ChurchId = church.Id,
            Title = "Creation",
            BibleReference = "Genesis 1",
            Theme = "God's Power and Love",
            AgeGroup = ages5To10.Name,
            StoryContent = "In the beginning, God created the heavens and the earth...",
            LearningObjective = "Understand that God made everything with love and purpose.",
            Activity = "Draw your favorite part of creation.",
            Prayer = "Thank You, God, for making our beautiful world.",
            IsPublished = true,
            Status = LessonStatus.Published,
            MemoryVerse = new MemoryVerse
            {
                VerseText = "In the beginning God created the heaven and the earth.",
                BibleReference = "Genesis 1:1",
                ShortExplanation = "God is the creator of everything."
            },
            Quiz = new Quiz
            {
                Title = "Creation Quiz",
                Description = "Test what you learned about creation.",
                Questions =
                [
                    new QuizQuestion
                    {
                        QuestionText = "On what day did God create light?",
                        QuestionType = QuestionType.SingleChoice,
                        Points = 1,
                        Options =
                        [
                            new QuizOption { OptionText = "Day 1", IsCorrect = true },
                            new QuizOption { OptionText = "Day 3", IsCorrect = false },
                            new QuizOption { OptionText = "Day 6", IsCorrect = false }
                        ]
                    },
                    new QuizQuestion
                    {
                        QuestionText = "God rested on the seventh day.",
                        QuestionType = QuestionType.TrueFalse,
                        Points = 1,
                        Options =
                        [
                            new QuizOption { OptionText = "True", IsCorrect = true },
                            new QuizOption { OptionText = "False", IsCorrect = false }
                        ]
                    }
                ]
            }
        };

        var davidAndGoliath = new Lesson
        {
            ChurchId = church.Id,
            Title = "David and Goliath",
            BibleReference = "1 Samuel 17",
            Theme = "Faith and Courage",
            AgeGroup = ages5To10.Name,
            StoryContent = "David, a young shepherd boy, trusted God to defeat the giant Goliath...",
            LearningObjective = "Learn that God gives us courage to face big challenges.",
            Activity = "Make a sling craft out of paper.",
            Prayer = "Lord, help me be brave and trust You like David did.",
            IsPublished = true,
            Status = LessonStatus.Published,
            MemoryVerse = new MemoryVerse
            {
                VerseText = "The battle is the Lord's.",
                BibleReference = "1 Samuel 17:47",
                ShortExplanation = "God fights for us when we trust Him."
            },
            Quiz = new Quiz
            {
                Title = "David and Goliath Quiz",
                Description = "Test what you learned about David's faith.",
                Questions =
                [
                    new QuizQuestion
                    {
                        QuestionText = "What weapon did David use to defeat Goliath?",
                        QuestionType = QuestionType.SingleChoice,
                        Points = 1,
                        Options =
                        [
                            new QuizOption { OptionText = "A sword", IsCorrect = false },
                            new QuizOption { OptionText = "A sling and stone", IsCorrect = true },
                            new QuizOption { OptionText = "A spear", IsCorrect = false }
                        ]
                    }
                ]
            }
        };

        var jesusCalmsStorm = new Lesson
        {
            ChurchId = church.Id,
            Title = "Jesus Calms the Storm",
            BibleReference = "Mark 4:35-41",
            Theme = "Trusting Jesus",
            AgeGroup = ages5To10.Name,
            StoryContent = "A great storm arose while Jesus and His disciples were in a boat...",
            LearningObjective = "Learn that Jesus is always with us, even in scary times.",
            Activity = "Make a paper boat craft.",
            Prayer = "Jesus, help me trust You when I am afraid.",
            IsPublished = false,
            Status = LessonStatus.Draft,
            MemoryVerse = new MemoryVerse
            {
                VerseText = "Peace, be still.",
                BibleReference = "Mark 4:39",
                ShortExplanation = "Jesus has power over everything, even the wind and waves."
            },
            Quiz = new Quiz
            {
                Title = "Jesus Calms the Storm Quiz",
                Description = "Test what you learned about trusting Jesus.",
                Questions =
                [
                    new QuizQuestion
                    {
                        QuestionText = "What did Jesus say to calm the storm?",
                        QuestionType = QuestionType.FillInTheBlank,
                        Points = 1,
                        Options =
                        [
                            new QuizOption { OptionText = "Peace, be still", IsCorrect = true }
                        ]
                    }
                ]
            }
        };

        db.Lessons.AddRange(creation, davidAndGoliath, jesusCalmsStorm);
        db.LessonClassGroups.Add(new LessonClassGroup { Lesson = creation, ClassGroup = ages5To10 });
        db.LessonClassGroups.Add(new LessonClassGroup { Lesson = davidAndGoliath, ClassGroup = ages5To10 });

        await db.SaveChangesAsync();
    }
}
