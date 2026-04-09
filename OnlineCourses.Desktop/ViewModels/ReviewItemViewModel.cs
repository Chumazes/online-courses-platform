namespace OnlineCourses.Desktop.ViewModels;

public sealed class ReviewItemViewModel
{
    public int ReviewId { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime ReviewDate { get; init; }
    public bool IsApproved { get; init; }

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

    public string CommentText =>
        string.IsNullOrWhiteSpace(Comment)
            ? "Пользователь пока не добавил текстовый комментарий."
            : Comment;
}
