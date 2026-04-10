using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LandingPage : Page
{
    private readonly LandingViewModel _viewModel;
    private readonly Action _openLogin;
    private readonly Action _openRegister;

    public LandingPage(CoursesClient coursesClient, Action openLogin, Action openRegister)
    {
        InitializeComponent();
        _viewModel = new LandingViewModel(coursesClient);
        _openLogin = openLogin;
        _openRegister = openRegister;
        DataContext = _viewModel;
        Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Page_Loaded;
        await _viewModel.LoadAsync();
    }

    private void LoginButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openLogin();
    }

    private void RegisterButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openRegister();
    }
}
