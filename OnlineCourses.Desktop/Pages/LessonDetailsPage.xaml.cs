using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LessonDetailsPage : Page
{
    private readonly FilesClient _filesClient;
    private readonly LessonDetailsViewModel _viewModel;

    public LessonDetailsPage(CourseLessonViewModel lesson, ProgressClient progressClient, FilesClient filesClient)
    {
        InitializeComponent();
        _filesClient = filesClient;
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
        var errorMessage = FileActionHelper.TryOpen(_viewModel.AttachmentDisplayUrl);
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return;
        }

        MessageBox.Show(
            errorMessage,
            "Файл недоступен",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void DownloadAttachmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await FileActionHelper.TryDownloadAsync(
            _filesClient,
            _viewModel.AttachmentFileUrl,
            _viewModel.AttachmentName);

        if (result.Success)
        {
            MessageBox.Show(
                $"Файл сохранён:\n{result.SavedPath}",
                "Скачивание завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return;
        }

        MessageBox.Show(
            result.ErrorMessage,
            "Не удалось скачать файл",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
