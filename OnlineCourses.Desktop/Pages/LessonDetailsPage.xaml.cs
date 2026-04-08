using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LessonDetailsPage : Page
{
    private readonly LessonDetailsViewModel _viewModel;

    public LessonDetailsPage(CourseLessonViewModel lesson, ProgressClient progressClient, FilesClient filesClient)
    {
        InitializeComponent();
        _viewModel = new LessonDetailsViewModel(lesson, progressClient, filesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadProgressAsync();
    }

    private void OpenAttachmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.AttachmentDisplayUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(_viewModel.AttachmentDisplayUrl)
        {
            UseShellExecute = true
        });
    }
}
