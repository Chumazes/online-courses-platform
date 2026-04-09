namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCourseItemViewModel
{
    public int CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Level { get; init; } = "beginner";
    public decimal Price { get; init; }
    public string Status { get; init; } = "draft";
    public string? CoverImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public int TotalStudents { get; init; }

    public string StatusText =>
        Status.ToLowerInvariant() switch
        {
            "draft" => "Черновик",
            "published" => "Опубликован",
            "archived" => "Архив",
            _ => string.IsNullOrWhiteSpace(Status) ? "Без статуса" : Status
        };

    public string MetaText
    {
        get
        {
            var parts = new List<string>
            {
                Level,
                $"{Price:0.##} ₽",
                $"{TotalStudents} студентов"
            };

            if (!string.IsNullOrWhiteSpace(AuthorName))
            {
                parts.Add($"Автор: {AuthorName}");
            }

            return string.Join(" • ", parts);
        }
    }
}
