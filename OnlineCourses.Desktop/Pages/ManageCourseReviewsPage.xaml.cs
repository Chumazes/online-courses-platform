using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ManageCourseReviewsPage : Page
{
    private readonly ManageCourseReviewsViewModel _viewModel;

    public ManageCourseReviewsPage(ManageCourseItemViewModel course, ReviewsClient reviewsClient, bool canModerate)
    {
        InitializeComponent();
        _viewModel = new ManageCourseReviewsViewModel(course, reviewsClient, canModerate);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void ApproveReviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.ApproveSelectedAsync(true);
    }

    private async void RejectReviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.ApproveSelectedAsync(false);
    }

    private async void DeleteReviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedReview is null)
        {
            MessageBox.Show(
                "Сначала выбери отзыв, который нужно удалить.",
                "Отзыв не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Удалить отзыв пользователя \"{_viewModel.SelectedReview.UserName}\"?",
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
