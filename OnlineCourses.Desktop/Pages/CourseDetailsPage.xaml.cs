using System.Windows.Controls;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CourseDetailsPage : Page
{
    public CourseDetailsPage(CourseCardViewModel course)
    {
        InitializeComponent();
        DataContext = new CourseDetailsViewModel(course);
    }
}
