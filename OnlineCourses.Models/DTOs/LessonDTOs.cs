namespace OnlineCourses.Models.DTOs;

public class CreateLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string LessonType { get; set; } = "video";
    public string? VideoUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int LessonOrder { get; set; }
    public bool IsFree { get; set; } = false;
}

public class UpdateLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string LessonType { get; set; } = "video";
    public string? VideoUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int LessonOrder { get; set; }
    public bool IsFree { get; set; } = false;
}

public class LessonResponseDto
{
    public int LessonId { get; set; }
    public int SectionId { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public int? DurationMinutes { get; set; }
    public int LessonOrder { get; set; }
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; }
}
