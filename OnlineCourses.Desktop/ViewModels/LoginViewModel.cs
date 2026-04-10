using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly AuthClient _authClient;
    private readonly Action _authorizedCallback;
    private readonly AsyncRelayCommand _submitCommand;
    private readonly RelayCommand _toggleModeCommand;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _fullName = string.Empty;
    private bool _isRegisterMode;
    private bool _isBusy;
    private string? _errorMessage;

    public LoginViewModel(AuthClient authClient, Action authorizedCallback)
    {
        _authClient = authClient;
        _authorizedCallback = authorizedCallback;

        _submitCommand = new AsyncRelayCommand(SubmitAsync, () => !IsBusy);
        _toggleModeCommand = new RelayCommand(_ => ToggleMode(), _ => !IsBusy);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        private set
        {
            if (SetProperty(ref _isRegisterMode, value))
            {
                RaisePropertyChanged(nameof(ModeTitle));
                RaisePropertyChanged(nameof(ModeActionText));
                RaisePropertyChanged(nameof(SwitchModeText));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _submitCommand.RaiseCanExecuteChanged();
                _toggleModeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string ModeTitle => IsRegisterMode ? "Регистрация" : "Вход";
    public string ModeActionText => IsRegisterMode ? "Создать аккаунт" : "Войти";
    public string SwitchModeText => IsRegisterMode ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Зарегистрироваться";
    public bool ShowFullName => IsRegisterMode;
    public ICommand SubmitCommand => _submitCommand;
    public ICommand ToggleModeCommand => _toggleModeCommand;

    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = null;
        RaisePropertyChanged(nameof(ShowFullName));
    }

    private async Task SubmitAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите email и пароль.";
            return;
        }

        if (IsRegisterMode && string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Введите ФИО.";
            return;
        }

        IsBusy = true;

        try
        {
            if (IsRegisterMode)
            {
                await _authClient.RegisterAsync(
                    new RegisterDto
                    {
                        Email = Email.Trim(),
                        Password = Password,
                        FullName = FullName.Trim()
                    });
            }

            await _authClient.LoginAsync(
                new LoginDto
                {
                    Email = Email.Trim(),
                    Password = Password
                });

            _authorizedCallback();
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось выполнить вход.", notifyUnauthorized: false);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось подключиться к API. Проверь, запущен ли сервер.";
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось выполнить вход.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
