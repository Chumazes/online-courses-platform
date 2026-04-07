using OnlineCourses.Client.Models;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ProfileViewModel : ViewModelBase
{
    public ProfileViewModel(CurrentUserDto user)
    {
        FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Имя не указано" : user.FullName;
        Email = user.Email;
        Role = FormatRole(user.Role);
        Bio = string.IsNullOrWhiteSpace(user.Bio)
            ? "Пока без описания. Здесь позже можно будет добавить bio и редактирование профиля."
            : user.Bio;
        Initials = GetInitials(FullName);
    }

    public string FullName { get; }
    public string Email { get; }
    public string Role { get; }
    public string Bio { get; }
    public string Initials { get; }

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
