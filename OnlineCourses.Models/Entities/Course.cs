namespace OnlineCourses.Models.Entities;

public class Course
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0;
    public string Level { get; set; } = "beginner";
    public string Status { get; set; } = "draft";
    public string? CoverImageUrl { get; set; }
    public decimal AvgRating { get; set; } = 0;

    public int? CategoryId { get; set; }
    public int AuthorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}