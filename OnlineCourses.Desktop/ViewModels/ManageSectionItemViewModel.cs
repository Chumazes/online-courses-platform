namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageSectionItemViewModel
{
    public int SectionId { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SectionOrder { get; init; }
    public int LessonsCount { get; init; }
    public DateTime CreatedAt { get; init; }

    public string OrderText => $"Раздел {SectionOrder}";

    public string MetaText => $"Уроков: {LessonsCount}";
}
