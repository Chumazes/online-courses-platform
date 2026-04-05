namespace OnlineCourses.Models.Entities;

public class Review
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}