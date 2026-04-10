namespace OnlineCourses.Desktop.ViewModels;

public sealed class LandingCourseCardViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = "Без категории";
    public string Level { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public int TotalStudents { get; init; }

    public string PriceCaption => Price <= 0 ? "Бесплатно" : $"{Price:0.##} ₽";

    public string MetaCaption =>
        string.IsNullOrWhiteSpace(AuthorName)
            ? $"{Level} • {TotalStudents} студентов"
            : $"{AuthorName} • {Level} • {TotalStudents} студентов";
}
