using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageSectionsPage : Page
{
    private readonly ManageSectionsViewModel _viewModel;

    public ManageSectionsPage(ManageCourseItemViewModel course, SectionsClient sectionsClient)
    {
        InitializeComponent();
        _viewModel = new ManageSectionsViewModel(course.CourseId, course.Title, sectionsClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
        if (_viewModel.Sections.Count == 0)
        {
            _viewModel.StartCreating();
        }
    }

    private void CreateSectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreating();
    }

    private async void DeleteSectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedSection is null)
        {
            MessageBox.Show(
                "Сначала выбери секцию, которую нужно удалить.",
                "Секция не выбрана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить секцию \"{_viewModel.SelectedSection.Title}\"?",
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
