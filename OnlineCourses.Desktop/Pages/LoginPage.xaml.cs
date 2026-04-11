using System.Windows;
using System.Windows.Controls;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class LoginPage : Page
{
    private readonly LoginViewModel _viewModel;
    private readonly Action? _onBack;

    public LoginPage(AuthClient authClient, Action onAuthorized, Action? onBack = null, bool startInRegisterMode = false)
    {
        InitializeComponent();
        _onBack = onBack;
        _viewModel = new LoginViewModel(authClient, onAuthorized, startInRegisterMode);
        DataContext = _viewModel;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
        }
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        _onBack?.Invoke();
    }
}
