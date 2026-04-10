namespace OnlineCourses.Desktop.ViewModels;

public sealed class RoleDashboardCourseViewModel
{
    public int CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal AvgRating { get; init; }
    public int TotalStudents { get; init; }
    public DateTime CreatedAt { get; init; }

    public string StatusText =>
        Status.ToLowerInvariant() switch
        {
            "draft" => "Черновик",
            "published" => "Опубликован",
            "archived" => "Архив",
            _ => string.IsNullOrWhiteSpace(Status) ? "Без статуса" : Status
        };

    public string RatingCaption => AvgRating <= 0 ? "Без оценок" : $"{AvgRating:0.0} / 5";

    public string MetaCaption
    {
        get
        {
            var parts = new List<string>
            {
                string.IsNullOrWhiteSpace(Level) ? "Без уровня" : Level,
                $"{TotalStudents} студентов",
                RatingCaption
            };

            if (!string.IsNullOrWhiteSpace(AuthorName))
            {
                parts.Add($"Автор: {AuthorName}");
            }

            return string.Join(" • ", parts);
        }
    }
}
