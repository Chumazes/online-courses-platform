namespace OnlineCourses.Models.DTOs;

public class CreateSectionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SectionOrder { get; set; }
}

public class UpdateSectionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SectionOrder { get; set; }
}

public class SectionResponseDto
{
    public int SectionId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SectionOrder { get; set; }
    public int LessonsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}