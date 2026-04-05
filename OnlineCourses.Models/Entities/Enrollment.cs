namespace OnlineCourses.Models.Entities;

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "active";
    public int OverallProgress { get; set; } = 0;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}