namespace OnlineCourses.Models.DTOs;

public class EnrollmentRequestDto
{
    public int CourseId { get; set; }
}

public class EnrollmentResponseDto
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OverallProgress { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CourseEnrollmentDto
{
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OverallProgress { get; set; }
}
