using System.Net.Http;
using System.Windows.Input;
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
    private string _fullName;
    private string _email;
    private string _role;
    private string _bio;
    private string _initials;
    private string? _avatarDisplayUrl;
    private bool _isSaving;
    private string? _statusMessage;
    private string? _errorMessage;

    public ProfileViewModel(
        CurrentUserDto user,
        AuthClient authClient,
        FilesClient filesClient,
        Action<CurrentUserDto> onProfileSaved)
    {
        _authClient = authClient;
        _filesClient = filesClient;
        _onProfileSaved = onProfileSaved;

        _fullName = "Имя не указано";
        _email = string.Empty;
        _role = string.Empty;
        _bio = string.Empty;
        _initials = "?";

        _saveCommand = new AsyncRelayCommand(SaveAsync, () => !IsSaving);
        ApplyUser(user);
    }

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

    public string? AvatarDisplayUrl
    {
        get => _avatarDisplayUrl;
        private set
        {
            if (SetProperty(ref _avatarDisplayUrl, value))
            {
                RaisePropertyChanged(nameof(HasAvatar));
            }
        }
    }

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarDisplayUrl);

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
            StatusMessage = "Аватар обновлён.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
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
            StatusMessage = "Профиль сохранён.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
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
        Role = FormatRole(user.Role);
        Bio = user.Bio ?? string.Empty;
        Initials = GetInitials(FullName);
        AvatarDisplayUrl = _filesClient.BuildDownloadUrl(user.AvatarUrl);
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
