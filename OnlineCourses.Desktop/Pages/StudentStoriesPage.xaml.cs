using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class StudentStoriesPage : Page
{
    private readonly StudentStoriesViewModel _viewModel;

    public StudentStoriesPage(
        CoursesClient coursesClient,
        ReviewsClient reviewsClient,
        FilesClient filesClient)
    {
        InitializeComponent();
        _viewModel = new StudentStoriesViewModel(coursesClient, reviewsClient, filesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }
}
