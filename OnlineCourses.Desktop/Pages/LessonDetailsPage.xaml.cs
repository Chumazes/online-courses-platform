using System.Windows.Controls;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LessonDetailsPage : Page
{
    public LessonDetailsPage(CourseLessonViewModel lesson)
    {
        InitializeComponent();
        DataContext = new LessonDetailsViewModel(lesson);
    }
}
