using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageLessonsPage : Page
{
    private readonly ManageLessonsViewModel _viewModel;

    public ManageLessonsPage(ManageSectionItemViewModel section, LessonsClient lessonsClient, FilesClient filesClient)
    {
        InitializeComponent();
        _viewModel = new ManageLessonsViewModel(section.SectionId, section.Title, lessonsClient, filesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        if (_viewModel.Lessons.Count == 0)
        {
            _viewModel.StartCreating();
        }
    }

    private void CreateLessonButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreating();
    }

    private async void UploadLessonFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanUploadFile)
        {
            MessageBox.Show(
                "Сначала выбери и сохрани урок, а потом загружай файл.",
                "Урок ещё не готов",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Выбери файл для урока",
            Filter = "Поддерживаемые файлы|*.pdf;*.doc;*.docx;*.zip;*.rar;*.mp4;*.txt;*.pptx;*.xlsx|Все файлы|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _viewModel.UploadFileAsync(dialog.FileName);
    }

    private void OpenUploadedFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.UploadedFileDisplayUrl))
        {
            MessageBox.Show(
                "Ссылка на загруженный файл пока недоступна.",
                "Файл недоступен",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo(_viewModel.UploadedFileDisplayUrl)
        {
            UseShellExecute = true
        });
    }

    private async void DeleteLessonButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedLesson is null)
        {
            MessageBox.Show(
                "Сначала выбери урок, который нужно удалить.",
                "Урок не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить урок \"{_viewModel.SelectedLesson.Title}\"?",
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
