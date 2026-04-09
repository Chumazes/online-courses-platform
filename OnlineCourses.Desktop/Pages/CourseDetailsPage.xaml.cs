using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class CourseDetailsPage : Page
{
    private readonly CourseDetailsViewModel _viewModel;
    private readonly Action<CourseLessonViewModel> _openLesson;

    public CourseDetailsPage(
        CourseCardViewModel course,
        CoursesClient coursesClient,
        EnrollmentsClient enrollmentsClient,
        ProgressClient progressClient,
        SectionsClient sectionsClient,
        LessonsClient lessonsClient,
        ReviewsClient reviewsClient,
        Action<CourseLessonViewModel> openLesson)
    {
        InitializeComponent();
        _viewModel = new CourseDetailsViewModel(
            course,
            coursesClient,
            enrollmentsClient,
            progressClient,
            sectionsClient,
            lessonsClient,
            reviewsClient);
        _openLesson = openLesson;
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private void LessonButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CourseLessonViewModel lesson })
        {
            _openLesson(lesson);
        }
    }
}
