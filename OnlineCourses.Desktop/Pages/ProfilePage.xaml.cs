using System.Windows.Controls;
using Microsoft.Win32;
using OnlineCourses.Client.Api;
using OnlineCourses.Client.Models;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ProfilePage : Page
{
    private readonly Action? _openDashboard;

    public ProfilePage(
        CurrentUserDto user,
        AuthClient authClient,
        FilesClient filesClient,
        string apiBaseUrl,
        Action<CurrentUserDto> onProfileSaved,
        Action? openDashboard = null)
    {
        InitializeComponent();
        _openDashboard = openDashboard;
        DataContext = new ProfileViewModel(user, authClient, filesClient, apiBaseUrl, onProfileSaved);
    }

    private async void UploadAvatarButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ProfileViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Выбери аватар",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await viewModel.UploadAvatarAsync(dialog.FileName);
    }

    private void OpenDashboardButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _openDashboard?.Invoke();
    }
}
