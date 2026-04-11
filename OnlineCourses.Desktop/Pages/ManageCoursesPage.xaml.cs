using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCoursesPage : Page
{
    private readonly ManageCoursesViewModel _viewModel;
    private readonly Action<ManageCourseItemViewModel> _openSections;
    private readonly Action<ManageCourseItemViewModel> _openStudents;
    private readonly Action<ManageCourseItemViewModel> _openAnalytics;
    private readonly Action<ManageCourseItemViewModel> _openReviews;
    private readonly Action _openCategories;

    public ManageCoursesPage(
        CoursesClient coursesClient,
        Action<ManageCourseItemViewModel> openSections,
        Action<ManageCourseItemViewModel> openStudents,
        Action<ManageCourseItemViewModel> openAnalytics,
        Action<ManageCourseItemViewModel> openReviews,
        Action openCategories,
        bool canModerateReviews,
        bool canManageCategories)
    {
        InitializeComponent();
        _openSections = openSections;
        _openStudents = openStudents;
        _openAnalytics = openAnalytics;
        _openReviews = openReviews;
        _openCategories = openCategories;
        _viewModel = new ManageCoursesViewModel(coursesClient, canModerateReviews);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
        ManageReviewsButton.Visibility = canModerateReviews ? Visibility.Visible : Visibility.Collapsed;
        ManageCategoriesButton.Visibility = canManageCategories ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private void CreateCourseButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreating();
    }

    private void ManageCategoriesButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openCategories();
    }

    private void ManageSectionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, для которого нужно открыть секции.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _openSections(_viewModel.SelectedCourse);
    }

    private void ManageStudentsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, для которого нужно открыть студентов.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _openStudents(_viewModel.SelectedCourse);
    }

    private void ManageAnalyticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, для которого нужно открыть аналитику.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _openAnalytics(_viewModel.SelectedCourse);
    }

    private void ManageReviewsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, для которого нужно открыть отзывы.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _openReviews(_viewModel.SelectedCourse);
    }

    private async void DeleteCourseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, который нужно удалить.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить курс \"{_viewModel.SelectedCourse.Title}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _viewModel.DeleteSelectedAsync();
    }
}
