using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Models.Entities;
using System.Data;

namespace OnlineCourses.API.Infrastructure;

public static class DevelopmentDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevelopmentDatabaseInitializer");

        var provider = (configuration["DatabaseProvider"] ?? "Postgres").ToLowerInvariant();

        if (provider == "sqlite")
        {
            await context.Database.EnsureCreatedAsync();
            await EnsureSqliteLessonFileColumnsAsync(context, logger);
            await SeedDevelopmentDataAsync(context, logger);
            return;
        }

        await context.Database.MigrateAsync();
        await EnsurePostgresLessonFileColumnsAsync(context, logger);
        await SeedDevelopmentDataAsync(context, logger);
    }

    private static async Task SeedDevelopmentDataAsync(AppDbContext context, ILogger logger)
    {
        var teacher = await EnsureUserAsync(context, "teacher@local.dev", "Demo Teacher", "teacher", "123456");
        var student = await EnsureUserAsync(context, "student@local.dev", "Demo Student", "student", "123456");
        await EnsureUserAsync(context, "admin@local.dev", "Demo Admin", "admin", "123456");
        var secondStudent = await EnsureUserAsync(context, "student2@local.dev", "Second Student", "student", "123456");

        var programming = await EnsureCategoryAsync(context, "Programming", "C#, ASP.NET Core, WPF and related courses.");
        var qaDevops = await EnsureCategoryAsync(context, "QA and DevOps", "Testing, automation, CI/CD and release practices.");

        var apiCourse = await EnsureCourseAsync(
            context,
            title: "ASP.NET Core Web API Basics",
            description: "Build REST endpoints, JWT authentication, EF Core data access and Swagger documentation.",
            price: 2990,
            level: "beginner",
            status: "published",
            authorId: teacher.UserId,
            categoryId: programming.CategoryId,
            avgRating: 4.8m);

        var qaCourse = await EnsureCourseAsync(
            context,
            title: "QA Automation for Online Courses API",
            description: "Practice API test cases, smoke checks, regression scenarios and defect reports.",
            price: 1990,
            level: "intermediate",
            status: "published",
            authorId: teacher.UserId,
            categoryId: qaDevops.CategoryId,
            avgRating: 4.6m);

        var wpfCourse = await EnsureCourseAsync(
            context,
            title: "WPF Desktop Client Essentials",
            description: "Connect a desktop client to the API, handle tokens, errors and async loading states.",
            price: 1490,
            level: "beginner",
            status: "published",
            authorId: teacher.UserId,
            categoryId: programming.CategoryId,
            avgRating: 4.4m);

        await EnsureCourseLessonsAsync(context, apiCourse, new[]
        {
            ("Project architecture", "Review API, Data, Models and Client projects.", "text", 1, true, (int?)null),
            ("CRUD endpoints", "Create, read, update and delete courses through controllers.", "video", 2, true, 18),
            ("JWT authentication", "Register, login, refresh and logout flows.", "text", 3, false, (int?)null),
            ("Swagger demo", "Prepare endpoints for defense and manual QA.", "video", 4, false, 14)
        });

        await EnsureCourseLessonsAsync(context, qaCourse, new[]
        {
            ("Smoke checklist", "Critical checks before every demo.", "text", 1, true, (int?)null),
            ("Postman collection", "Organize requests for auth, courses, lessons and reviews.", "text", 2, false, (int?)null),
            ("Bug report examples", "Write reproducible defects with expected and actual results.", "video", 3, false, 11)
        });

        await EnsureCourseLessonsAsync(context, wpfCourse, new[]
        {
            ("Login screen", "Token storage and user state in the desktop client.", "text", 1, true, (int?)null),
            ("Course catalog", "Load cards, categories and course details asynchronously.", "video", 2, false, 16)
        });

        var csharp = await EnsureTagAsync(context, "csharp", "C# language");
        var dotnet = await EnsureTagAsync(context, "dotnet", ".NET platform");
        var testing = await EnsureTagAsync(context, "testing", "QA and test automation");
        var devops = await EnsureTagAsync(context, "devops", "CI/CD and deployment");
        var wpf = await EnsureTagAsync(context, "wpf", "Desktop client");

        await EnsureCourseTagAsync(context, apiCourse.CourseId, csharp.TagId);
        await EnsureCourseTagAsync(context, apiCourse.CourseId, dotnet.TagId);
        await EnsureCourseTagAsync(context, qaCourse.CourseId, testing.TagId);
        await EnsureCourseTagAsync(context, qaCourse.CourseId, devops.TagId);
        await EnsureCourseTagAsync(context, wpfCourse.CourseId, wpf.TagId);

        var apiEnrollment = await EnsureEnrollmentAsync(context, student.UserId, apiCourse.CourseId, "active", 50);
        var qaEnrollment = await EnsureEnrollmentAsync(context, student.UserId, qaCourse.CourseId, "active", 30);
        await EnsureEnrollmentAsync(context, secondStudent.UserId, apiCourse.CourseId, "active", 25);

        await EnsureLessonProgressAsync(context, apiEnrollment, completedCount: 2);
        await EnsureLessonProgressAsync(context, qaEnrollment, completedCount: 1);

        await EnsureReviewAsync(context, student.UserId, apiCourse.CourseId, 5, "Clear course for API defense preparation.", isApproved: true);
        await EnsureReviewAsync(context, secondStudent.UserId, apiCourse.CourseId, 4, "Useful examples for local testing.", isApproved: true);
        await EnsureReviewAsync(context, student.UserId, qaCourse.CourseId, 5, "Good QA checklist and regression ideas.", isApproved: true);

        logger.LogInformation("Development database is ready with seeded demo data.");
    }

    private static async Task EnsureSqliteLessonFileColumnsAsync(AppDbContext context, ILogger logger)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info('Lessons');";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            var alterStatements = new List<string>();

            if (!existingColumns.Contains("FileName"))
            {
                alterStatements.Add("ALTER TABLE Lessons ADD COLUMN FileName TEXT NULL;");
            }

            if (!existingColumns.Contains("FileUrl"))
            {
                alterStatements.Add("ALTER TABLE Lessons ADD COLUMN FileUrl TEXT NULL;");
            }

            if (!existingColumns.Contains("FileType"))
            {
                alterStatements.Add("ALTER TABLE Lessons ADD COLUMN FileType TEXT NULL;");
            }

            if (!existingColumns.Contains("FileSize"))
            {
                alterStatements.Add("ALTER TABLE Lessons ADD COLUMN FileSize INTEGER NULL;");
            }

            foreach (var statement in alterStatements)
            {
                await using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = statement;
                await alterCommand.ExecuteNonQueryAsync();
            }

            if (alterStatements.Count > 0)
            {
                logger.LogInformation("SQLite Lessons table was updated with lesson file metadata columns.");
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsurePostgresLessonFileColumnsAsync(AppDbContext context, ILogger logger)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            var statements = new[]
            {
                "ALTER TABLE \"Lessons\" ADD COLUMN IF NOT EXISTS \"FileName\" text NULL;",
                "ALTER TABLE \"Lessons\" ADD COLUMN IF NOT EXISTS \"FileUrl\" text NULL;",
                "ALTER TABLE \"Lessons\" ADD COLUMN IF NOT EXISTS \"FileType\" text NULL;",
                "ALTER TABLE \"Lessons\" ADD COLUMN IF NOT EXISTS \"FileSize\" bigint NULL;"
            };

            foreach (var statement in statements)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                await command.ExecuteNonQueryAsync();
            }

            logger.LogInformation("PostgreSQL Lessons table is ready with lesson file metadata columns.");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<User> EnsureUserAsync(
        AppDbContext context,
        string email,
        string fullName,
        string role,
        string password)
    {
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User
        {
            Email = email,
            FullName = fullName,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Category> EnsureCategoryAsync(AppDbContext context, string name, string description)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (category is not null)
        {
            return category;
        }

        category = new Category
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<Tag> EnsureTagAsync(AppDbContext context, string name, string description)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Name == name);
        if (tag is not null)
        {
            return tag;
        }

        tag = new Tag
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        return tag;
    }

    private static async Task<Course> EnsureCourseAsync(
        AppDbContext context,
        string title,
        string description,
        decimal price,
        string level,
        string status,
        int authorId,
        int categoryId,
        decimal avgRating)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Title == title);
        if (course is not null)
        {
            return course;
        }

        course = new Course
        {
            Title = title,
            Description = description,
            Price = price,
            Level = level,
            Status = status,
            AuthorId = authorId,
            CategoryId = categoryId,
            AvgRating = avgRating,
            CreatedAt = DateTime.UtcNow
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();
        return course;
    }

    private static async Task EnsureCourseLessonsAsync(
        AppDbContext context,
        Course course,
        IEnumerable<(string Title, string Content, string Type, int Order, bool IsFree, int? Duration)> lessons)
    {
        if (await context.Sections.AnyAsync(s => s.CourseId == course.CourseId))
        {
            return;
        }

        var basics = new Section
        {
            CourseId = course.CourseId,
            Title = "Basics",
            Description = "Core theory and first practical checks.",
            SectionOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var practice = new Section
        {
            CourseId = course.CourseId,
            Title = "Practice",
            Description = "Hands-on tasks for API and client testing.",
            SectionOrder = 2,
            CreatedAt = DateTime.UtcNow
        };

        context.Sections.AddRange(basics, practice);
        await context.SaveChangesAsync();

        foreach (var lesson in lessons)
        {
            context.Lessons.Add(new Lesson
            {
                SectionId = lesson.Order <= 2 ? basics.SectionId : practice.SectionId,
                Title = lesson.Title,
                Content = lesson.Content,
                LessonType = lesson.Type,
                DurationMinutes = lesson.Duration,
                LessonOrder = lesson.Order,
                IsFree = lesson.IsFree,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureCourseTagAsync(AppDbContext context, int courseId, int tagId)
    {
        if (await context.CourseTags.AnyAsync(ct => ct.CourseId == courseId && ct.TagId == tagId))
        {
            return;
        }

        context.CourseTags.Add(new CourseTag { CourseId = courseId, TagId = tagId });
        await context.SaveChangesAsync();
    }

    private static async Task<Enrollment> EnsureEnrollmentAsync(
        AppDbContext context,
        int userId,
        int courseId,
        string status,
        int overallProgress)
    {
        var enrollment = await context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        if (enrollment is not null)
        {
            return enrollment;
        }

        enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            Status = status,
            OverallProgress = overallProgress,
            EnrollmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();
        return enrollment;
    }

    private static async Task EnsureLessonProgressAsync(AppDbContext context, Enrollment enrollment, int completedCount)
    {
        var lessonIds = await context.Sections
            .Where(s => s.CourseId == enrollment.CourseId)
            .OrderBy(s => s.SectionOrder)
            .SelectMany(s => s.Lessons.OrderBy(l => l.LessonOrder))
            .Select(l => l.LessonId)
            .ToListAsync();

        for (var index = 0; index < lessonIds.Count; index++)
        {
            var lessonId = lessonIds[index];
            if (await context.LessonProgresses.AnyAsync(p => p.EnrollmentId == enrollment.EnrollmentId && p.LessonId == lessonId))
            {
                continue;
            }

            var isCompleted = index < completedCount;
            context.LessonProgresses.Add(new LessonProgress
            {
                EnrollmentId = enrollment.EnrollmentId,
                LessonId = lessonId,
                IsCompleted = isCompleted,
                WatchTime = isCompleted ? 900 : 180,
                LastAccessed = DateTime.UtcNow,
                CompletedAt = isCompleted ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureReviewAsync(
        AppDbContext context,
        int userId,
        int courseId,
        int rating,
        string comment,
        bool isApproved)
    {
        if (await context.Reviews.AnyAsync(r => r.UserId == userId && r.CourseId == courseId))
        {
            return;
        }

        context.Reviews.Add(new Review
        {
            UserId = userId,
            CourseId = courseId,
            Rating = rating,
            Comment = comment,
            IsApproved = isApproved,
            ReviewDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
