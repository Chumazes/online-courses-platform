namespace OnlineCourses.Desktop.ViewModels;

public sealed class LessonDetailsViewModel : ViewModelBase
{
    public LessonDetailsViewModel(CourseLessonViewModel lesson)
    {
        Title = lesson.Title;
        SectionTitle = string.IsNullOrWhiteSpace(lesson.SectionTitle)
            ? "Раздел не указан"
            : lesson.SectionTitle;
        MetaText = lesson.MetaText;
        AccessText = lesson.AccessText;
        Content = string.IsNullOrWhiteSpace(lesson.Content)
            ? "Содержимое урока пока не заполнено."
            : lesson.Content;
    }

    public string Title { get; }
    public string SectionTitle { get; }
    public string MetaText { get; }
    public string AccessText { get; }
    public string Content { get; }
}
