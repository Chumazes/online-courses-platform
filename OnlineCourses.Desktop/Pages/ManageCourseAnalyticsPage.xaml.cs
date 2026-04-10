using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCourseAnalyticsPage : Page
{
    private readonly ManageCourseAnalyticsViewModel _viewModel;

    public ManageCourseAnalyticsPage(
        ManageCourseItemViewModel course,
        EnrollmentsClient enrollmentsClient,
        ReviewsClient reviewsClient,
        FilesClient filesClient)
    {
        InitializeComponent();
        _viewModel = new ManageCourseAnalyticsViewModel(course, enrollmentsClient, reviewsClient, filesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }
}
