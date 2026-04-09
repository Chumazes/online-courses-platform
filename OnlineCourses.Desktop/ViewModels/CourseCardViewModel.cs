namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseCardViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Level { get; init; } = "beginner";
    public decimal Price { get; init; }
}
