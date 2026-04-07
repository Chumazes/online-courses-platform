using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CoursesPage : Page
{
    private readonly CoursesViewModel _viewModel;

    public CoursesPage(CoursesClient coursesClient, Action<CourseCardViewModel> openCourse)
    {
        InitializeComponent();
        _viewModel = new CoursesViewModel(coursesClient, openCourse);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadCoursesAsync();
    }
}
