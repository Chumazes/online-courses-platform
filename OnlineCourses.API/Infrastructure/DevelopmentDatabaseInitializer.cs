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
        var teacher = await EnsureUserAsync(context, "teacher@local.dev", "Иван Петров", "teacher", "123456");
        var student = await EnsureUserAsync(context, "student@local.dev", "Анна Смирнова", "student", "123456");
        await EnsureUserAsync(context, "admin@local.dev", "Администратор платформы", "admin", "123456");
        var secondStudent = await EnsureUserAsync(context, "student2@local.dev", "Дмитрий Волков", "student", "123456");

        var programming = await EnsureCategoryAsync(context, "Программирование", "C#, ASP.NET Core, WPF и практическая разработка.");
        var qaDevops = await EnsureCategoryAsync(context, "QA и DevOps", "Тестирование, автоматизация, CI/CD и развертывание.");

        var apiCourse = await EnsureCourseAsync(
            context,
            title: "ASP.NET Core Web API с нуля",
            description: "Практический курс по созданию REST API, JWT-авторизации, Entity Framework Core и Swagger-документации.",
            price: 2990,
            level: "beginner",
            status: "published",
            authorId: teacher.UserId,
            categoryId: programming.CategoryId,
            avgRating: 4.8m);

        var qaCourse = await EnsureCourseAsync(
            context,
            title: "Автотесты и QA для REST API",
            description: "Разбор smoke-проверок, тест-кейсов, Postman-коллекций, регрессионных сценариев и баг-репортов.",
            price: 1990,
            level: "intermediate",
            status: "published",
            authorId: teacher.UserId,
            categoryId: qaDevops.CategoryId,
            avgRating: 4.6m);

        var wpfCourse = await EnsureCourseAsync(
            context,
            title: "WPF-клиент для учебной платформы",
            description: "Создание настольного клиента, подключение к API, работа с токенами, ошибками и асинхронной загрузкой.",
            price: 1490,
            level: "beginner",
            status: "published",
            authorId: teacher.UserId,
            categoryId: programming.CategoryId,
            avgRating: 4.4m);

        await EnsureCourseLessonsAsync(context, apiCourse, new[]
        {
            ("Архитектура проекта", "Разбираем слои API, Data, Models и Client.", "text", 1, true, (int?)null),
            ("CRUD-эндпоинты", "Создаем, читаем, обновляем и удаляем курсы через контроллеры.", "video", 2, true, 18),
            ("JWT-авторизация", "Регистрация, вход, refresh token и выход из системы.", "text", 3, false, (int?)null),
            ("Демонстрация Swagger", "Готовим эндпоинты к защите и ручной проверке.", "video", 4, false, 14)
        });

        await EnsureCourseLessonsAsync(context, qaCourse, new[]
        {
            ("Smoke-чеклист", "Критические проверки перед каждым демо.", "text", 1, true, (int?)null),
            ("Postman-коллекция", "Организация запросов для авторизации, курсов, уроков и отзывов.", "text", 2, false, (int?)null),
            ("Примеры баг-репортов", "Описываем воспроизводимые дефекты с ожидаемым и фактическим результатом.", "video", 3, false, 11)
        });

        await EnsureCourseLessonsAsync(context, wpfCourse, new[]
        {
            ("Экран входа", "Хранение токенов и состояние пользователя в desktop-клиенте.", "text", 1, true, (int?)null),
            ("Каталог курсов", "Асинхронная загрузка карточек, категорий и деталей курса.", "video", 2, false, 16)
        });

        var csharp = await EnsureTagAsync(context, "csharp", "Язык программирования C#");
        var dotnet = await EnsureTagAsync(context, "dotnet", "Платформа .NET");
        var testing = await EnsureTagAsync(context, "testing", "QA и автоматизация тестирования");
        var devops = await EnsureTagAsync(context, "devops", "CI/CD и развертывание");
        var wpf = await EnsureTagAsync(context, "wpf", "Настольный клиент");

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

        await EnsureReviewAsync(context, student.UserId, apiCourse.CourseId, 5, "Понятный курс для подготовки API к защите.", isApproved: true);
        await EnsureReviewAsync(context, secondStudent.UserId, apiCourse.CourseId, 4, "Полезные примеры для локального тестирования.", isApproved: true);
        await EnsureReviewAsync(context, student.UserId, qaCourse.CourseId, 5, "Хороший чеклист QA и идеи для регрессионных проверок.", isApproved: true);

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
            Title = "Основы",
            Description = "Базовая теория и первые практические проверки.",
            SectionOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var practice = new Section
        {
            CourseId = course.CourseId,
            Title = "Практика",
            Description = "Практические задания для проверки API и клиентского приложения.",
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
