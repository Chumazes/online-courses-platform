using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void CoursesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(CoursesList, e.OriginalSource as DependencyObject) is not ListBoxItem)
        {
            return;
        }

        if (_viewModel.OpenCourseCommand.CanExecute(null))
        {
            _viewModel.OpenCourseCommand.Execute(null);
        }
    }
}
