using System.IO;
using System.Net.Http;
using System.Windows;
using OnlineCourses.Client.Api;
using OnlineCourses.Client.Infrastructure;
using OnlineCourses.Client.Models;
using OnlineCourses.Desktop.Pages;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthClient _authClient;
    private readonly CoursesClient _coursesClient;
    private readonly SectionsClient _sectionsClient;
    private readonly LessonsClient _lessonsClient;

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
        _sectionsClient = new SectionsClient(httpClient, tokenStore);
        _lessonsClient = new LessonsClient(httpClient, tokenStore);

        NavigateToLogin();
    }

    private void NavigateToLogin()
    {
        var page = new LoginPage(_authClient, NavigateToCourses);
        MainFrame.Navigate(page);
        ClearBackStack();
        ClearProfileHeader();
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
        SetProfileLoadingState();
        UpdateHeader(loggedIn: true, canGoBack: false);
        _ = LoadCurrentUserAsync();
    }

    private void NavigateToCourseDetails(CourseCardViewModel course)
    {
        MainFrame.Navigate(new CourseDetailsPage(course, _coursesClient, _sectionsClient, _lessonsClient));
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
        ProfileBadge.Visibility = loggedIn ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var user = await _authClient.GetCurrentUserAsync();
            ApplyProfileHeader(user);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await _authClient.TryRefreshAsync();
            if (refreshed)
            {
                try
                {
                    var user = await _authClient.GetCurrentUserAsync();
                    ApplyProfileHeader(user);
                    return;
                }
                catch
                {
                }
            }

            await PerformLogoutAsync();
        }
        catch
        {
            UserNameText.Text = "Профиль недоступен";
            UserRoleText.Text = "Не удалось загрузить";
            UserInitialsText.Text = "!";
        }
    }

    private void SetProfileLoadingState()
    {
        UserNameText.Text = "Загружаем профиль...";
        UserRoleText.Text = "Проверяем сессию";
        UserInitialsText.Text = "...";
    }

    private void ClearProfileHeader()
    {
        UserNameText.Text = "Профиль";
        UserRoleText.Text = "Не загружен";
        UserInitialsText.Text = "?";
    }

    private void ApplyProfileHeader(CurrentUserDto user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        UserNameText.Text = displayName;
        UserRoleText.Text = FormatRole(user.Role);
        UserInitialsText.Text = GetInitials(displayName);
    }

    private static string FormatRole(string role) =>
        role.ToLowerInvariant() switch
        {
            "student" => "Студент",
            "teacher" => "Преподаватель",
            "admin" => "Администратор",
            _ => role
        };

    private static string GetInitials(string value)
    {
        var parts = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();

        if (parts.Length == 0)
        {
            return "?";
        }

        return new string(parts);
    }
}
