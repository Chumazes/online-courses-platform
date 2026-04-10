using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCourseStudentsPage : Page
{
    private readonly ManageCourseStudentsViewModel _viewModel;

    public ManageCourseStudentsPage(
        ManageCourseItemViewModel course,
        EnrollmentsClient enrollmentsClient,
        FilesClient filesClient)
    {
        InitializeComponent();
        _viewModel = new ManageCourseStudentsViewModel(course, enrollmentsClient, filesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }
}
