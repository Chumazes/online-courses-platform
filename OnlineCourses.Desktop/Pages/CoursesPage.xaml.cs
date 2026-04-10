using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CoursesPage : Page
{
    private readonly CoursesViewModel _viewModel;
    private readonly Action _openMyCourses;

    public CoursesPage(
        CoursesClient coursesClient,
        Action<CourseCardViewModel> openCourse,
        Action openMyCourses)
    {
        InitializeComponent();
        _viewModel = new CoursesViewModel(coursesClient, openCourse);
        _openMyCourses = openMyCourses;
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    public void SetMyCoursesVisibility(bool isVisible)
    {
        MyCoursesButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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

    private void MyCoursesButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openMyCourses();
    }
}
