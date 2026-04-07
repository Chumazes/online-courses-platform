using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CourseDetailsPage : Page
{
    private readonly CourseDetailsViewModel _viewModel;

    public CourseDetailsPage(
        CourseCardViewModel course,
        CoursesClient coursesClient,
        SectionsClient sectionsClient,
        LessonsClient lessonsClient)
    {
        InitializeComponent();
        _viewModel = new CourseDetailsViewModel(course, coursesClient, sectionsClient, lessonsClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }
}
