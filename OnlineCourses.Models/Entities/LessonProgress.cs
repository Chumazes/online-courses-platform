using System.ComponentModel.DataAnnotations;

namespace OnlineCourses.Models.Entities;

public class LessonProgress
{
    [Key]
    public int ProgressId { get; set; }
    public int EnrollmentId { get; set; }
    public int LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchTime { get; set; }
    public DateTime? LastAccessed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Enrollment Enrollment { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}