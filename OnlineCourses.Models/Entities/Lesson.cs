namespace OnlineCourses.Models.Entities;

public class Lesson
{
    public int LessonId { get; set; }
    public int SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string LessonType { get; set; } = "video";
    public string? VideoUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int LessonOrder { get; set; }
    public bool IsFree { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Section Section { get; set; } = null!;
}