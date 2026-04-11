using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCategoriesPage : Page
{
    private readonly ManageCategoriesViewModel _viewModel;

    public ManageCategoriesPage(CoursesClient coursesClient)
    {
        InitializeComponent();
        _viewModel = new ManageCategoriesViewModel(coursesClient);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        if (_viewModel.Categories.Count == 0)
        {
            _viewModel.StartCreating();
        }
    }

    private void CreateCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartCreating();
    }

    private async void DeleteCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCategory is null)
        {
            MessageBox.Show(
                "Сначала выбери категорию, которую нужно удалить.",
                "Категория не выбрана",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить категорию \"{_viewModel.SelectedCategory.Name}\"?",
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
