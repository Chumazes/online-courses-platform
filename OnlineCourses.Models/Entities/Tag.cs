namespace OnlineCourses.Models.Entities;

public class Tag
{
    public int TagId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
}