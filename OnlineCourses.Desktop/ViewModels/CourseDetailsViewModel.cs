namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseDetailsViewModel : ViewModelBase
{
    public CourseDetailsViewModel(CourseCardViewModel course)
    {
        Title = course.Title;
        Description = course.Description;
        Level = course.Level;
        Price = course.Price == 0 ? "Бесплатно" : $"{course.Price:0.##} ₽";
    }

    public string Title { get; }
    public string Description { get; }
    public string Level { get; }
    public string Price { get; }
}
