namespace OnlineCourses.Desktop.ViewModels;

public sealed class RoleDashboardCourseViewModel
{
    public int CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal AvgRating { get; init; }
    public int TotalStudents { get; init; }
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? CoverImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool ShowModerationAction { get; init; }

    public string StatusText =>
        Status.ToLowerInvariant() switch
        {
            "draft" => "Черновик",
            "published" => "Опубликован",
            "archived" => "Архив",
            _ => string.IsNullOrWhiteSpace(Status) ? "Без статуса" : Status
        };

    public string RatingCaption => AvgRating <= 0 ? "Без оценок" : $"{AvgRating:0.0} / 5";

    public string StudentsCaption => TotalStudents == 1 ? "1 студент" : $"{TotalStudents} студентов";

    public string PriceCaption => Price <= 0 ? "Бесплатно" : $"{Price:0.##} ₽";

    public string ActionHint =>
        Status.Equals("draft", StringComparison.OrdinalIgnoreCase)
            ? "Курс ещё в черновике. Проверь программу и публикацию."
            : TotalStudents == 0
                ? "Пока нет студентов. Стоит усилить подачу и структуру курса."
                : AvgRating <= 0
                    ? "Студенты уже есть, но отзывов пока нет."
                    : "Курс в работе. Можно следить за прогрессом и отзывами.";

    public string MetaCaption
    {
        get
        {
            var parts = new List<string>
            {
                string.IsNullOrWhiteSpace(Level) ? "Без уровня" : Level,
                PriceCaption,
                StudentsCaption,
                RatingCaption
            };

            if (!string.IsNullOrWhiteSpace(CategoryName))
            {
                parts.Add($"Категория: {CategoryName}");
            }

            if (!string.IsNullOrWhiteSpace(AuthorName))
            {
                parts.Add($"Автор: {AuthorName}");
            }

            return string.Join(" • ", parts);
        }
    }

    public ManageCourseItemViewModel ToManageCourseItem() =>
        new()
        {
            CourseId = CourseId,
            Title = Title,
            Description = Description,
            AuthorName = AuthorName,
            Level = Level,
            Price = Price,
            Status = Status,
            CategoryId = CategoryId,
            CategoryName = CategoryName,
            CoverImageUrl = CoverImageUrl,
            CreatedAt = CreatedAt,
            TotalStudents = TotalStudents
        };
}
