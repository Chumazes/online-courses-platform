namespace OnlineCourses.Models.DTOs;

public class UpdateProgressDto
{
    public int LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchTime { get; set; } // в секундах
}

public class LessonProgressResponseDto
{
    public int ProgressId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int WatchTime { get; set; }
    public DateTime? LastAccessed { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CourseProgressResponseDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int OverallProgress { get; set; } // 0-100
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}