using System.Net.Http;
using System.Windows.Input;
using System.Windows.Media;
using OnlineCourses.Client.Api;
using OnlineCourses.Client.Models;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ProfileViewModel : ViewModelBase
{
    private readonly AuthClient _authClient;
    private readonly FilesClient _filesClient;
    private readonly Action<CurrentUserDto> _onProfileSaved;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly string _apiBaseUrl;
    private string _fullName;
    private string _email;
    private string _role;
    private string _roleKey;
    private string _bio;
    private string _initials;
    private ImageSource? _avatarImageSource;
    private bool _isSaving;
    private string? _statusMessage;
    private string? _errorMessage;

    public ProfileViewModel(
        CurrentUserDto user,
        AuthClient authClient,
        FilesClient filesClient,
        string apiBaseUrl,
        Action<CurrentUserDto> onProfileSaved)
    {
        _authClient = authClient;
        _filesClient = filesClient;
        _apiBaseUrl = apiBaseUrl;
        _onProfileSaved = onProfileSaved;

        _fullName = "Имя не указано";
        _email = string.Empty;
        _role = string.Empty;
        _roleKey = string.Empty;
        _bio = string.Empty;
        _initials = "?";

        _saveCommand = new AsyncRelayCommand(SaveAsync, () => !IsSaving);
        ApplyUser(user);
    }

    public string ApiBaseUrl => _apiBaseUrl;

    public bool CanOpenDashboard =>
        _roleKey.Equals("teacher", StringComparison.OrdinalIgnoreCase) ||
        _roleKey.Equals("admin", StringComparison.OrdinalIgnoreCase);

    public string DashboardButtonText =>
        _roleKey.Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? "Открыть панель администратора"
            : "Открыть панель преподавателя";

    public string FullName
    {
        get => _fullName;
        private set => SetProperty(ref _fullName, value);
    }

    public string Email
    {
        get => _email;
        private set => SetProperty(ref _email, value);
    }

    public string Role
    {
        get => _role;
        private set => SetProperty(ref _role, value);
    }

    public string Bio
    {
        get => _bio;
        set => SetProperty(ref _bio, value);
    }

    public string Initials
    {
        get => _initials;
        private set => SetProperty(ref _initials, value);
    }

    public ImageSource? AvatarImageSource
    {
        get => _avatarImageSource;
        private set
        {
            if (SetProperty(ref _avatarImageSource, value))
            {
                RaisePropertyChanged(nameof(HasAvatar));
            }
        }
    }

    public bool HasAvatar => AvatarImageSource is not null;

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(SaveButtonText));
            }
        }
    }

    public string SaveButtonText => IsSaving ? "Сохраняем..." : "Сохранить bio";

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SaveCommand => _saveCommand;

    public async Task UploadAvatarAsync(string filePath)
    {
        IsSaving = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            await _filesClient.UploadAvatarAsync(filePath);
            var updatedUser = await _authClient.GetCurrentUserAsync();
            ApplyUser(updatedUser);
            _onProfileSaved(updatedUser);
            StatusMessage = "Аватар обновлен.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить аватар.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить аватар. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task SaveAsync()
    {
        IsSaving = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            var updatedUser = await _authClient.UpdateProfileAsync(new UpdateProfileDto
            {
                Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim()
            });

            ApplyUser(updatedUser);
            _onProfileSaved(updatedUser);
            StatusMessage = "Профиль сохранен.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось сохранить профиль.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось сохранить профиль. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ApplyUser(CurrentUserDto user)
    {
        FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Имя не указано" : user.FullName;
        Email = user.Email;
        _roleKey = user.Role ?? string.Empty;
        Role = FormatRole(user.Role ?? string.Empty);
        Bio = user.Bio ?? string.Empty;
        Initials = GetInitials(FullName);
        AvatarImageSource = ImageSourceFactory.Create(_filesClient.BuildDownloadUrl(user.AvatarUrl));
        RaisePropertyChanged(nameof(CanOpenDashboard));
        RaisePropertyChanged(nameof(DashboardButtonText));
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
