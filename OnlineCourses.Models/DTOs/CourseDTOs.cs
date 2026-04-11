namespace OnlineCourses.Models.DTOs;

public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0;
    public string Level { get; set; } = "beginner";
    public int? CategoryId { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Level { get; set; } = "beginner";
    public string Status { get; set; } = "draft";
    public int? CategoryId { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class CourseResponseDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal AvgRating { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalStudents { get; set; }
}

public class CourseCategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}

public class CreateCourseCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}

public class UpdateCourseCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}
