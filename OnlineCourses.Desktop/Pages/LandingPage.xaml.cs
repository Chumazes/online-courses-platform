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
    private readonly Action _openWhyIt;
    private readonly Action _openFaq;

    public LandingPage(
        CoursesClient coursesClient,
        Action openLogin,
        Action openRegister,
        Action openWhyIt,
        Action openFaq)
    {
        InitializeComponent();
        _viewModel = new LandingViewModel(coursesClient);
        _openLogin = openLogin;
        _openRegister = openRegister;
        _openWhyIt = openWhyIt;
        _openFaq = openFaq;
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

    private void WhyItButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openWhyIt();
    }

    private void FaqButton_OnClick(object sender, RoutedEventArgs e)
    {
        _openFaq();
    }
}
