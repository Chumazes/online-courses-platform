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
            await SeedSqliteDevelopmentDataAsync(context, logger);
            return;
        }

        await context.Database.MigrateAsync();
    }

    private static async Task SeedSqliteDevelopmentDataAsync(AppDbContext context, ILogger logger)
    {
        var teacher = await EnsureUserAsync(
            context,
            email: "teacher@local.dev",
            fullName: "Demo Teacher",
            role: "teacher",
            password: "123456");

        await EnsureUserAsync(
            context,
            email: "student@local.dev",
            fullName: "Demo Student",
            role: "student",
            password: "123456");

        await EnsureUserAsync(
            context,
            email: "admin@local.dev",
            fullName: "Demo Admin",
            role: "admin",
            password: "123456");

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Frontend");
        if (category is null)
        {
            category = new Category
            {
                Name = "Frontend",
                Description = "Категория для локальной dev-проверки"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var mainCourse = await context.Courses.FirstOrDefaultAsync(c => c.Title == "WPF для начинающих");
        if (mainCourse is null)
        {
            mainCourse = new Course
            {
                Title = "WPF для начинающих",
                Description = "Тестовый курс для локальной проверки desktop-клиента без PostgreSQL.",
                Price = 0,
                Level = "beginner",
                Status = "published",
                AuthorId = teacher.UserId,
                CategoryId = category.CategoryId
            };

            context.Courses.Add(mainCourse);
            await context.SaveChangesAsync();
        }

        if (!await context.Sections.AnyAsync(s => s.CourseId == mainCourse.CourseId))
        {
            var introSection = new Section
            {
                CourseId = mainCourse.CourseId,
                Title = "Старт",
                Description = "Вводная часть по проекту и интерфейсу.",
                SectionOrder = 1
            };

            var layoutSection = new Section
            {
                CourseId = mainCourse.CourseId,
                Title = "Верстка и привязки",
                Description = "Базовая работа с XAML, Binding и структурой страницы.",
                SectionOrder = 2
            };

            context.Sections.AddRange(introSection, layoutSection);
            await context.SaveChangesAsync();

            context.Lessons.AddRange(
                new Lesson
                {
                    SectionId = introSection.SectionId,
                    Title = "Как устроен WPF-проект",
                    Content = "Разбираем окно, страницы и ViewModel.",
                    LessonType = "text",
                    LessonOrder = 1,
                    IsFree = true
                },
                new Lesson
                {
                    SectionId = introSection.SectionId,
                    Title = "Первый экран курсов",
                    Content = "Смотрим, как список курсов приходит из API.",
                    LessonType = "video",
                    DurationMinutes = 12,
                    LessonOrder = 2,
                    IsFree = true
                },
                new Lesson
                {
                    SectionId = layoutSection.SectionId,
                    Title = "Binding без боли",
                    Content = "Учимся связывать данные из ViewModel с XAML.",
                    LessonType = "text",
                    LessonOrder = 1,
                    IsFree = false
                },
                new Lesson
                {
                    SectionId = layoutSection.SectionId,
                    Title = "Карточка курса и детали",
                    Content = "Подключаем реальные секции и уроки на экран курса.",
                    LessonType = "video",
                    DurationMinutes = 18,
                    LessonOrder = 2,
                    IsFree = false
                });

            await context.SaveChangesAsync();
        }

        var emptyCourse = await context.Courses.FirstOrDefaultAsync(c => c.Title == "UI-полировка приложения");
        if (emptyCourse is null)
        {
            emptyCourse = new Course
            {
                Title = "UI-полировка приложения",
                Description = "Второй тестовый курс. У него пока нет разделов, чтобы можно было проверить пустое состояние.",
                Price = 1490,
                Level = "intermediate",
                Status = "published",
                AuthorId = teacher.UserId,
                CategoryId = category.CategoryId
            };

            context.Courses.Add(emptyCourse);
            await context.SaveChangesAsync();
        }

        logger.LogInformation("SQLite development database is ready with seeded demo data.");
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
}
