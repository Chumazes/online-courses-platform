using System.Collections.ObjectModel;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseSectionViewModel
{
    public int SectionId { get; init; }
    public int SectionOrder { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ObservableCollection<CourseLessonViewModel> Lessons { get; } = new();

    public string LessonsCaption =>
        Lessons.Count == 1 ? "1 урок" : $"{Lessons.Count} уроков";
}
