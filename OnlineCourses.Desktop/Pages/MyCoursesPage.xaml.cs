using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class MyCoursesPage : Page
{
    private readonly MyCoursesViewModel _viewModel;

    public MyCoursesPage(
        EnrollmentsClient enrollmentsClient,
        Action<CourseCardViewModel> openCourse)
    {
        InitializeComponent();
        _viewModel = new MyCoursesViewModel(enrollmentsClient, openCourse);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }

    private void MyCoursesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(MyCoursesList, e.OriginalSource as DependencyObject) is not ListBoxItem)
        {
            return;
        }

        if (_viewModel.OpenCourseCommand.CanExecute(null))
        {
            _viewModel.OpenCourseCommand.Execute(null);
        }
    }
}
