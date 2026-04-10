using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected string GetFriendlyApiError(
        ApiException ex,
        string fallbackMessage,
        bool notifyUnauthorized = true)
    {
        switch (ex.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
            {
                const string message = "Сессия истекла. Выполните вход снова.";
                if (notifyUnauthorized)
                {
                    SessionEvents.RaiseSessionExpired(message);
                }

                return message;
            }

            case HttpStatusCode.Forbidden:
                return TryExtractApiMessage(ex.ResponseBody) ?? "Недостаточно прав для этого действия.";

            case HttpStatusCode.NotFound:
                return TryExtractApiMessage(ex.ResponseBody) ?? "Запрошенные данные не найдены.";

            default:
                return TryExtractApiMessage(ex.ResponseBody) ?? fallbackMessage;
        }
    }

    protected static string GetFriendlyConnectionError(string fallbackMessage)
    {
        return fallbackMessage;
    }

    protected static string GetFriendlyUnexpectedError(Exception ex, string fallbackMessage)
    {
        return string.IsNullOrWhiteSpace(ex.Message) ? fallbackMessage : ex.Message;
    }

    protected static string? TryExtractApiMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("message", out var messageElement))
                {
                    var message = NormalizeApiMessage(messageElement.GetString());
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }

                if (document.RootElement.TryGetProperty("detail", out var detailElement))
                {
                    var detail = NormalizeApiMessage(detailElement.GetString());
                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        return detail;
                    }
                }

                if (document.RootElement.TryGetProperty("errors", out var errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Object)
                {
                    var errors = new List<string>();
                    foreach (var property in errorsElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in property.Value.EnumerateArray())
                            {
                                var error = NormalizeApiMessage(item.GetString());
                                if (!string.IsNullOrWhiteSpace(error))
                                {
                                    errors.Add(error);
                                }
                            }
                        }
                        else
                        {
                            var error = NormalizeApiMessage(property.Value.GetString());
                            if (!string.IsNullOrWhiteSpace(error))
                            {
                                errors.Add(error);
                            }
                        }
                    }

                    if (errors.Count > 0)
                    {
                        return string.Join(Environment.NewLine, errors.Distinct());
                    }
                }

                if (document.RootElement.TryGetProperty("title", out var titleElement))
                {
                    var title = NormalizeApiMessage(titleElement.GetString());
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return title;
                    }
                }
            }
        }
        catch
        {
        }

        return NormalizeApiMessage(responseBody);
    }

    private static string? NormalizeApiMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var message = value.Trim();
        var normalized = message.ToLowerInvariant();

        if (normalized is "one or more validation errors occurred." or "one or more validation errors occurred")
        {
            return "Проверьте заполнение полей.";
        }

        if (normalized.Contains("invalid credentials") || normalized.Contains("invalid email or password"))
        {
            return "Неверный email или пароль.";
        }

        if (normalized.Contains("email") && normalized.Contains("valid e-mail address"))
        {
            return "Введите корректный email.";
        }

        if (normalized.Contains("email") && normalized.Contains("required"))
        {
            return "Введите email.";
        }

        if (normalized.Contains("password") && normalized.Contains("required"))
        {
            return "Введите пароль.";
        }

        if ((normalized.Contains("fullname") || normalized.Contains("full name")) && normalized.Contains("required"))
        {
            return "Введите имя.";
        }

        return message;
    }
}
