using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using OnlineCourses.Client.Api;
using OnlineCourses.Client.Infrastructure;
using OnlineCourses.Client.Models;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Desktop.Pages;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthClient _authClient;
    private readonly CoursesClient _coursesClient;
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly ProgressClient _progressClient;
    private readonly SectionsClient _sectionsClient;
    private readonly LessonsClient _lessonsClient;
    private readonly FilesClient _filesClient;
    private readonly string _apiBaseUrl;
    private CurrentUserDto? _currentUser;

    public MainWindow()
    {
        InitializeComponent();

        var settings = DesktopSettingsLoader.Load();
        _apiBaseUrl = settings.ApiBaseUrl;
        var httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };

        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var sessionFilePath = Path.Combine(appDataFolder, "OnlineCourses", "session.json");

        var tokenStore = new FileTokenStore(sessionFilePath);
        _authClient = new AuthClient(httpClient, tokenStore);
        _coursesClient = new CoursesClient(httpClient, tokenStore);
        _enrollmentsClient = new EnrollmentsClient(httpClient, tokenStore);
        _progressClient = new ProgressClient(httpClient, tokenStore);
        _sectionsClient = new SectionsClient(httpClient, tokenStore);
        _lessonsClient = new LessonsClient(httpClient, tokenStore);
        _filesClient = new FilesClient(httpClient, tokenStore);
        MainFrame.Navigated += MainFrame_OnNavigated;

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
            openMyCourses: NavigateToMyCourses);

        MainFrame.Navigate(page);
        ClearBackStack();
        SetProfileLoadingState();
        UpdateHeader(loggedIn: true, canGoBack: false);
        _ = LoadCurrentUserAsync();
    }

    private void NavigateToMyCourses()
    {
        MainFrame.Navigate(new MyCoursesPage(_enrollmentsClient, NavigateToCourseDetails));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageCourses()
    {
        MainFrame.Navigate(new ManageCoursesPage(_coursesClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToCourseDetails(CourseCardViewModel course)
    {
        MainFrame.Navigate(new CourseDetailsPage(
            course,
            _coursesClient,
            _enrollmentsClient,
            _progressClient,
            _sectionsClient,
            _lessonsClient,
            NavigateToLessonDetails));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToLessonDetails(CourseLessonViewModel lesson)
    {
        MainFrame.Navigate(new LessonDetailsPage(lesson, _progressClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private async void ProfileBadge_OnClick(object sender, RoutedEventArgs e)
    {
        var user = _currentUser ?? await EnsureCurrentUserAsync();
        if (user is null)
        {
            MessageBox.Show(
                "Не удалось загрузить профиль. Проверь, доступен ли API и действительна ли сессия.",
                "Профиль недоступен",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        MainFrame.Navigate(new ProfilePage(user, _authClient, _filesClient, _apiBaseUrl, OnProfileSaved));
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

    private void ManageCoursesButton_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateToManageCourses();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }
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
        ManageCoursesButton.Visibility = loggedIn && CanManageCourses()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void MainFrame_OnNavigated(object? sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        var loggedIn = MainFrame.Content is not LoginPage;
        var canGoBack = loggedIn && MainFrame.Content is not CoursesPage && MainFrame.CanGoBack;
        UpdateHeader(loggedIn, canGoBack);
    }

    private async Task LoadCurrentUserAsync()
    {
        var user = await EnsureCurrentUserAsync();
        if (user is null)
        {
            UserNameText.Text = "Профиль недоступен";
            UserRoleText.Text = "Не удалось загрузить";
            UserInitialsText.Text = "!";
        }
    }

    private async Task<CurrentUserDto?> EnsureCurrentUserAsync()
    {
        try
        {
            var user = await _authClient.GetCurrentUserAsync();
            ApplyProfileHeader(user);
            return user;
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await PerformLogoutAsync();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void SetProfileLoadingState()
    {
        UserNameText.Text = "Загружаем профиль...";
        UserRoleText.Text = "Проверяем сессию";
        UserInitialsText.Text = "...";
        ManageCoursesButton.Visibility = Visibility.Collapsed;
    }

    private void ClearProfileHeader()
    {
        _currentUser = null;
        UserNameText.Text = "Профиль";
        UserRoleText.Text = "Не загружен";
        UserInitialsText.Text = "?";
        UserAvatarImage.Source = null;
        UserAvatarImage.Visibility = Visibility.Collapsed;
        UserInitialsText.Visibility = Visibility.Visible;
        ManageCoursesButton.Visibility = Visibility.Collapsed;
    }

    private void ApplyProfileHeader(CurrentUserDto user)
    {
        _currentUser = user;
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        UserNameText.Text = displayName;
        UserRoleText.Text = FormatRole(user.Role);
        UserInitialsText.Text = GetInitials(displayName);
        ApplyHeaderAvatar(user.AvatarUrl);
        UpdateHeader(loggedIn: true, canGoBack: MainFrame.Content is not LoginPage && MainFrame.Content is not CoursesPage && MainFrame.CanGoBack);
    }

    private void OnProfileSaved(CurrentUserDto user)
    {
        ApplyProfileHeader(user);
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

    private bool CanManageCourses()
    {
        var role = _currentUser?.Role;
        return role is not null &&
               (role.Equals("teacher", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("admin", StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyHeaderAvatar(string? avatarUrl)
    {
        var avatarSource = _filesClient.BuildDownloadUrl(avatarUrl);
        if (string.IsNullOrWhiteSpace(avatarSource))
        {
            UserAvatarImage.Source = null;
            UserAvatarImage.Visibility = Visibility.Collapsed;
            UserInitialsText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            UserAvatarImage.Source = new BitmapImage(new Uri(avatarSource));
            UserAvatarImage.Visibility = Visibility.Visible;
            UserInitialsText.Visibility = Visibility.Collapsed;
        }
        catch
        {
            UserAvatarImage.Source = null;
            UserAvatarImage.Visibility = Visibility.Collapsed;
            UserInitialsText.Visibility = Visibility.Visible;
        }
    }
}
