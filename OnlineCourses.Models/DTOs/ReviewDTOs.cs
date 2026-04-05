using System.ComponentModel.DataAnnotations;

namespace OnlineCourses.Models.DTOs;

public class CreateReviewDto
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string? Comment { get; set; }
}

public class UpdateReviewDto
{
    [Range(1, 5)]
    public int? Rating { get; set; }
    
    public string? Comment { get; set; }
}

public class ReviewResponseDto
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewDate { get; set; }
    public bool IsApproved { get; set; }
}

public class CourseRatingDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}