using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class RoleDashboardPage : Page
{
    private readonly RoleDashboardViewModel _viewModel;
    private readonly Action _openManageCourses;

    public RoleDashboardPage(
        CoursesClient coursesClient,
        bool showAllCourses,
        bool isAdmin,
        Action openManageCourses)
    {
        InitializeComponent();
        _viewModel = new RoleDashboardViewModel(coursesClient, showAllCourses, isAdmin);
        _openManageCourses = openManageCourses;
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }

    private void OpenManageCoursesButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openManageCourses();
    }
}
