using System.Windows.Media;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseEnrollmentItemViewModel
{
    public int EnrollmentId { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public ImageSource? AvatarSource { get; init; }
    public DateTime EnrollmentDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int OverallProgress { get; init; }

    public bool HasAvatar => AvatarSource is not null;

    public bool ShowInitials => AvatarSource is null;

    public string Initials
    {
        get
        {
            var parts = UserName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]))
                .ToArray();

            return parts.Length == 0 ? "?" : new string(parts);
        }
    }

    public string StatusText =>
        Status.ToLowerInvariant() switch
        {
            "active" => "Активный",
            "completed" => "Завершён",
            "expired" => "Неактивный",
            _ => string.IsNullOrWhiteSpace(Status) ? "Без статуса" : Status
        };

    public string EnrollmentCaption => $"Записан: {EnrollmentDate:dd.MM.yyyy}";

    public string ProgressCaption => $"Прогресс: {OverallProgress}%";
}
