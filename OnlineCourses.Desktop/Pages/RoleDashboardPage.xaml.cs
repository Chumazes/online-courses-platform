using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class RoleDashboardPage : Page
{
    private readonly RoleDashboardViewModel _viewModel;
    private readonly Action _openManageCourses;
    private readonly Action<ManageCourseItemViewModel> _openStudents;
    private readonly Action<ManageCourseItemViewModel> _openAnalytics;
    private readonly Action<ManageCourseItemViewModel>? _openReviews;

    public RoleDashboardPage(
        CoursesClient coursesClient,
        bool showAllCourses,
        bool isAdmin,
        Action openManageCourses,
        Action<ManageCourseItemViewModel> openStudents,
        Action<ManageCourseItemViewModel> openAnalytics,
        Action<ManageCourseItemViewModel>? openReviews)
    {
        InitializeComponent();
        _viewModel = new RoleDashboardViewModel(coursesClient, showAllCourses, isAdmin);
        _openManageCourses = openManageCourses;
        _openStudents = openStudents;
        _openAnalytics = openAnalytics;
        _openReviews = openReviews;
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

    private void OpenStudentsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: RoleDashboardCourseViewModel course })
        {
            _openStudents(course.ToManageCourseItem());
        }
    }

    private void OpenAnalyticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: RoleDashboardCourseViewModel course })
        {
            _openAnalytics(course.ToManageCourseItem());
        }
    }

    private void OpenReviewsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_openReviews is null)
        {
            return;
        }

        if (sender is FrameworkElement { Tag: RoleDashboardCourseViewModel course })
        {
            _openReviews(course.ToManageCourseItem());
        }
    }
}
