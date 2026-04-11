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
    private readonly ReviewsClient _reviewsClient;
    private readonly FilesClient _filesClient;
    private readonly string _apiBaseUrl;
    private CurrentUserDto? _currentUser;
    private bool _isHandlingSessionExpired;

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
        _reviewsClient = new ReviewsClient(httpClient, tokenStore);
        _filesClient = new FilesClient(httpClient, tokenStore);
        MainFrame.Navigated += MainFrame_OnNavigated;
        SessionEvents.SessionExpired += OnSessionExpired;
        Closed += MainWindow_OnClosed;

        NavigateToLanding();
    }

    private void NavigateToLanding()
    {
        var page = new LandingPage(
            _coursesClient,
            openLogin: () => NavigateToLogin(false),
            openRegister: () => NavigateToLogin(true),
            openWhyIt: NavigateToWhyIt,
            openFaq: NavigateToFaq,
            openStories: NavigateToStudentStories);
        MainFrame.Navigate(page);
        ClearBackStack();
        ClearProfileHeader();
        UpdateHeader(loggedIn: false, canGoBack: false);
    }

    private void NavigateToWhyIt()
    {
        MainFrame.Navigate(new WhyItPage());
        UpdateHeader(loggedIn: false, canGoBack: true);
    }

    private void NavigateToFaq()
    {
        MainFrame.Navigate(new FaqPage());
        UpdateHeader(loggedIn: false, canGoBack: true);
    }

    private void NavigateToStudentStories()
    {
        MainFrame.Navigate(new StudentStoriesPage(_coursesClient, _reviewsClient, _filesClient));
        UpdateHeader(loggedIn: false, canGoBack: true);
    }

    private void NavigateToLogin(bool startInRegisterMode = false)
    {
        var page = new LoginPage(_authClient, NavigateToCourses, startInRegisterMode);
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
        page.SetMyCoursesVisibility(CanOpenMyCourses());

        MainFrame.Navigate(page);
        ClearBackStack();
        SetProfileLoadingState();
        UpdateHeader(loggedIn: true, canGoBack: false);
        _ = LoadCurrentUserAsync();
    }

    private void NavigateToMyCourses()
    {
        MainFrame.Navigate(new MyCoursesPage(_enrollmentsClient, _progressClient, NavigateToCourseDetails));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageCourses()
    {
        if (MainFrame.Content is ManageCoursesPage or ManageSectionsPage or ManageLessonsPage or ManageCourseReviewsPage or ManageCourseStudentsPage or ManageCourseAnalyticsPage)
        {
            return;
        }

        MainFrame.Navigate(new ManageCoursesPage(
            _coursesClient,
            NavigateToManageSections,
            NavigateToManageCourseStudents,
            NavigateToManageCourseAnalytics,
            NavigateToManageCourseReviews,
            CanModerateReviews()));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageSections(ManageCourseItemViewModel course)
    {
        MainFrame.Navigate(new ManageSectionsPage(course, _sectionsClient, NavigateToManageLessons));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageLessons(ManageSectionItemViewModel section)
    {
        MainFrame.Navigate(new ManageLessonsPage(section, _lessonsClient, _filesClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageCourseReviews(ManageCourseItemViewModel course)
    {
        MainFrame.Navigate(new ManageCourseReviewsPage(course, _reviewsClient, _filesClient, CanModerateReviews()));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageCourseStudents(ManageCourseItemViewModel course)
    {
        MainFrame.Navigate(new ManageCourseStudentsPage(course, _enrollmentsClient, _filesClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToManageCourseAnalytics(ManageCourseItemViewModel course)
    {
        MainFrame.Navigate(new ManageCourseAnalyticsPage(course, _enrollmentsClient, _reviewsClient, _filesClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToRoleDashboard()
    {
        MainFrame.Navigate(new RoleDashboardPage(
            _coursesClient,
            showAllCourses: CanModerateReviews(),
            isAdmin: CanModerateReviews(),
            openManageCourses: NavigateToManageCourses,
            openStudents: NavigateToManageCourseStudents,
            openAnalytics: NavigateToManageCourseAnalytics,
            openReviews: CanModerateReviews() ? NavigateToManageCourseReviews : null));
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
            _reviewsClient,
            _filesClient,
            NavigateToLessonDetails));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private void NavigateToLessonDetails(CourseLessonViewModel lesson)
    {
        MainFrame.Navigate(new LessonDetailsPage(lesson, _progressClient, _filesClient));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private async void ProfileBadge_OnClick(object sender, RoutedEventArgs e)
    {
        var user = _currentUser ?? await EnsureCurrentUserAsync();
        if (IsPublicPage(MainFrame.Content))
        {
            return;
        }

        if (user is null)
        {
            MessageBox.Show(
                "Не удалось загрузить профиль. Проверь, доступен ли API и действительна ли сессия.",
                "Профиль недоступен",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        MainFrame.Navigate(new ProfilePage(
            user,
            _authClient,
            _filesClient,
            _apiBaseUrl,
            OnProfileSaved,
            CanManageCourses() ? NavigateToRoleDashboard : null));
        UpdateHeader(loggedIn: true, canGoBack: true);
    }

    private async Task PerformLogoutAsync()
    {
        await _authClient.LogoutAsync();
        NavigateToLanding();
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
        ManageCoursesButton.Visibility = loggedIn && CanManageCourses() && !IsManagementPage()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void MainFrame_OnNavigated(object? sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        var loggedIn = !IsPublicPage(MainFrame.Content);
        var canGoBack = loggedIn && MainFrame.Content is not CoursesPage && MainFrame.CanGoBack;
        if (!loggedIn && MainFrame.Content is WhyItPage or FaqPage or StudentStoriesPage)
        {
            canGoBack = MainFrame.CanGoBack;
        }
        UpdateCoursesPageRoleVisibility();
        UpdateHeader(loggedIn, canGoBack);
    }

    private async Task LoadCurrentUserAsync()
    {
        var user = await EnsureCurrentUserAsync();
        if (IsPublicPage(MainFrame.Content))
        {
            return;
        }

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
            SessionEvents.RaiseSessionExpired("Сессия истекла. Выполните вход снова.");
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
        UpdateCoursesPageRoleVisibility();
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
        UpdateCoursesPageRoleVisibility();
    }

    private void ApplyProfileHeader(CurrentUserDto user)
    {
        _currentUser = user;
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        UserNameText.Text = displayName;
        UserRoleText.Text = FormatRole(user.Role);
        UserInitialsText.Text = GetInitials(displayName);
        ApplyHeaderAvatar(user.AvatarUrl);
        UpdateCoursesPageRoleVisibility();
        UpdateHeader(loggedIn: true, canGoBack: !IsPublicPage(MainFrame.Content) && MainFrame.Content is not CoursesPage && MainFrame.CanGoBack);
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

    private bool CanOpenMyCourses()
    {
        var role = _currentUser?.Role;
        return role is not null &&
               role.Equals("student", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanModerateReviews()
    {
        var role = _currentUser?.Role;
        return role is not null &&
               role.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateCoursesPageRoleVisibility()
    {
        if (MainFrame.Content is CoursesPage coursesPage)
        {
            coursesPage.SetMyCoursesVisibility(CanOpenMyCourses());
        }
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

    private bool IsManagementPage()
    {
        return MainFrame.Content is ManageCoursesPage or ManageSectionsPage or ManageLessonsPage or ManageCourseReviewsPage or ManageCourseStudentsPage or ManageCourseAnalyticsPage or RoleDashboardPage;
    }

    private static bool IsPublicPage(object? content)
    {
        return content is LoginPage or LandingPage or WhyItPage or FaqPage or StudentStoriesPage;
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        SessionEvents.SessionExpired -= OnSessionExpired;
        Closed -= MainWindow_OnClosed;
    }

    private async void OnSessionExpired(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => OnSessionExpired(message));
            return;
        }

        if (_isHandlingSessionExpired)
        {
            return;
        }

        _isHandlingSessionExpired = true;
        try
        {
            await _authClient.LogoutAsync();
            if (!IsPublicPage(MainFrame.Content))
            {
                NavigateToLanding();
                MessageBox.Show(
                    message,
                    "Сессия завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        finally
        {
            _isHandlingSessionExpired = false;
        }
    }
}
