namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageLessonItemViewModel
{
    public int LessonId { get; init; }
    public int SectionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string LessonType { get; init; } = "text";
    public string? VideoUrl { get; init; }
    public string? FileName { get; init; }
    public string? FileUrl { get; init; }
    public string? FileType { get; init; }
    public long? FileSize { get; init; }
    public int? DurationMinutes { get; init; }
    public int LessonOrder { get; init; }
    public bool IsFree { get; init; }
    public DateTime CreatedAt { get; init; }

    public string OrderText => $"Урок {LessonOrder}";

    public string MetaText
    {
        get
        {
            var duration = DurationMinutes is > 0 ? $"{DurationMinutes} мин" : "без длительности";
            var access = IsFree ? "бесплатный" : "по подписке";
            return $"{NormalizeLessonType(LessonType)} • {duration} • {access}";
        }
    }

    private static string NormalizeLessonType(string lessonType) =>
        lessonType.ToLowerInvariant() switch
        {
            "video" => "Видео",
            "text" => "Текст",
            "quiz" => "Тест",
            _ => string.IsNullOrWhiteSpace(lessonType) ? "Урок" : lessonType
        };
}
