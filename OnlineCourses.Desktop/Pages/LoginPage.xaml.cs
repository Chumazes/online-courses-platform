using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LoginPage : Page
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(AuthClient authClient, Action onAuthorized)
    {
        InitializeComponent();
        _viewModel = new LoginViewModel(authClient, onAuthorized);
        DataContext = _viewModel;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
        }
    }
}
