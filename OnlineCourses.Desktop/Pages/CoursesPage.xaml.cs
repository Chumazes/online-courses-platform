using System.Windows.Controls;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CoursesPage : Page
{
    public CoursesPage(Action<CourseCardViewModel> openCourse, Func<Task> logout)
    {
        InitializeComponent();
        DataContext = new CoursesViewModel(openCourse, logout);
    }
}
