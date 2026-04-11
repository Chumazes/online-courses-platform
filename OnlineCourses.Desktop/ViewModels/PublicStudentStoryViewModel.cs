using System.Windows.Media;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class PublicStudentStoryViewModel
{
    public string UserName { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public int Rating { get; init; }
    public DateTime ReviewDate { get; init; }
    public ImageSource? AvatarSource { get; init; }

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

    public string RatingText => new string('★', Rating) + new string('☆', Math.Max(0, 5 - Rating));

    public string ReviewDateText => ReviewDate == default ? "Без даты" : ReviewDate.ToLocalTime().ToString("dd.MM.yyyy");

    public string SafeComment =>
        string.IsNullOrWhiteSpace(Comment)
            ? "Студент пока не оставил текстовый комментарий, но поставил оценку курсу."
            : Comment;
}
