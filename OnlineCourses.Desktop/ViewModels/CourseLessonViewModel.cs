namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseLessonViewModel
{
    public int LessonId { get; init; }
    public int LessonOrder { get; init; }
    public string SectionTitle { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string LessonType { get; init; } = string.Empty;
    public bool IsFree { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Content { get; init; }

    public string MetaText
    {
        get
        {
            var duration = DurationMinutes.HasValue ? $"{DurationMinutes.Value} мин" : "без длительности";
            var access = IsFree ? "бесплатный" : "по записи";
            return $"{LessonType} • {duration} • {access}";
        }
    }

    public string AccessText => IsFree ? "Открытый урок" : "Урок для записанных студентов";
}
