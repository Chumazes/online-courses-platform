using System.IO;
using System.Net.Http;
using System.Windows;
using OnlineCourses.Client.Api;
using OnlineCourses.Client.Infrastructure;
using OnlineCourses.Desktop.Pages;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthClient _authClient;
    private readonly CoursesClient _coursesClient;

    public MainWindow()
    {
        InitializeComponent();

        var baseUrl = "http://localhost:5064/";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var sessionFilePath = Path.Combine(appDataFolder, "OnlineCourses", "session.json");

        var tokenStore = new FileTokenStore(sessionFilePath);
        _authClient = new AuthClient(httpClient, tokenStore);
        _coursesClient = new CoursesClient(httpClient, tokenStore);

        NavigateToLogin();
    }

    private void NavigateToLogin()
    {
        var page = new LoginPage(_authClient, NavigateToCourses);
        MainFrame.Navigate(page);
        ClearBackStack();
        UpdateHeader(loggedIn: false, canGoBack: false);
    }

    private void NavigateToCourses()
    {
        var page = new CoursesPage(
            _coursesClient,
            openCourse: NavigateToCourseDetails,
            logout: PerformLogoutAsync);

        MainFrame.Navigate(page);
        ClearBackStack();
        UpdateHeader(loggedIn: true, canGoBack: false);
    }

    private void NavigateToCourseDetails(CourseCardViewModel course)
    {
        MainFrame.Navigate(new CourseDetailsPage(course));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private async Task PerformLogoutAsync()
    {
        await _authClient.LogoutAsync();
        NavigateToLogin();
    }

    private async void LogoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PerformLogoutAsync();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }

        UpdateHeader(loggedIn: true, canGoBack: MainFrame.CanGoBack);
    }

    private void ClearBackStack()
    {
        while (MainFrame.CanGoBack)
        {
            MainFrame.RemoveBackEntry();
        }
    }

    private void UpdateHeader(bool loggedIn, bool canGoBack)
    {
        LogoutButton.Visibility = loggedIn ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = canGoBack ? Visibility.Visible : Visibility.Collapsed;
    }
}
