using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class MyCoursesPage : Page
{
    private readonly MyCoursesViewModel _viewModel;

    public MyCoursesPage(
        EnrollmentsClient enrollmentsClient,
        ProgressClient progressClient,
        Action<CourseCardViewModel> openCourse)
    {
        InitializeComponent();
        _viewModel = new MyCoursesViewModel(enrollmentsClient, progressClient, openCourse);
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }

    private void MyCoursesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(MyCoursesList, e.OriginalSource as DependencyObject) is not ListBoxItem)
        {
            return;
        }

        if (_viewModel.OpenCourseCommand.CanExecute(null))
        {
            _viewModel.OpenCourseCommand.Execute(null);
        }
    }

    private async void UnenrollButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCourse is null)
        {
            MessageBox.Show(
                "Сначала выбери курс, от которого хочешь отписаться.",
                "Курс не выбран",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Отписаться от курса \"{_viewModel.SelectedCourse.Title}\"?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await _viewModel.UnenrollSelectedAsync();
    }
}
