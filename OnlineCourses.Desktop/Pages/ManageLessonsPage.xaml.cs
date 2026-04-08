using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageLessonsPage : Page
{
    private readonly ManageLessonsViewModel _viewModel;

    public ManageLessonsPage(ManageSectionItemViewModel section, LessonsClient lessonsClient)
    {
        InitializeComponent();
        _viewModel = new ManageLessonsViewModel(section.SectionId, section.Title, lessonsClient);
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
