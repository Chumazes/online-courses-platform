namespace OnlineCourses.Desktop.ViewModels;

public sealed class MyCourseCardViewModel
{
    public int CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OverallProgress { get; init; }
    public int TotalLessons { get; init; }
    public int CompletedLessons { get; init; }
    public DateTime EnrollmentDate { get; init; }
    public DateTime? CompletedAt { get; init; }

    public string StatusText =>
        Status.ToLowerInvariant() switch
        {
            "active" => "Активный",
            "completed" => "Завершён",
            "expired" => "Неактивный",
            _ => string.IsNullOrWhiteSpace(Status) ? "Без статуса" : Status
        };

    public string EnrollmentCaption => $"Запись: {EnrollmentDate:dd.MM.yyyy}";

    public string ProgressCaption =>
        CompletedAt.HasValue || OverallProgress >= 100
            ? "Курс завершён"
            : $"Прогресс: {OverallProgress}%";

    public string LessonsCaption =>
        TotalLessons <= 0
            ? "Уроки пока не загружены"
            : $"{CompletedLessons} из {TotalLessons} уроков завершено";

    public bool CanUnenroll =>
        !string.Equals(Status, "expired", StringComparison.OrdinalIgnoreCase) &&
        !CompletedAt.HasValue;

    public CourseCardViewModel ToCourseCard() =>
        new()
        {
            Id = CourseId,
            Title = Title,
            Description = "Загружаем описание курса...",
            Level = "enrolled",
            Price = 0
        };
}
