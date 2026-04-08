using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCoursesPage : Page
{
    private readonly ManageCoursesViewModel _viewModel;

    public ManageCoursesPage(CoursesClient coursesClient)
    {
        InitializeComponent();
        _viewModel = new ManageCoursesViewModel(coursesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }

    private void CreateCourseButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreating();
    }

    private async void DeleteCourseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, который нужно удалить.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить курс \"{_viewModel.SelectedCourse.Title}\"?",
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
