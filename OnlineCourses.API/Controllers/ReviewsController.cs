using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ReviewsController> _logger;
    
    public ReviewsController(
        IReviewRepository reviewRepository,
        ICourseRepository courseRepository,
        ICacheService cacheService,
        ILogger<ReviewsController> logger)
    {
        _reviewRepository = reviewRepository;
        _courseRepository = courseRepository;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    // GET: api/reviews/course/{courseId}
    [HttpGet("course/{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseReviews(int courseId, [FromQuery] bool all = false)
    {
        _logger.LogInformation("Getting reviews for course {CourseId}, all={All}", courseId, all);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found for reviews: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        var cacheKey = $"course_reviews_{courseId}_{all}";
        var cachedReviews = _cacheService.Get<List<ReviewResponseDto>>(cacheKey);
        
        if (cachedReviews != null)
        {
            _logger.LogDebug("Returning cached reviews for course {CourseId}", courseId);
            return Ok(cachedReviews);
        }
        
        var reviews = await _reviewRepository.GetByCourseIdAsync(courseId, !all);
        var response = reviews.Select(r => MapToResponseDto(r)).ToList();
        
        _cacheService.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("Found {Count} reviews for course {CourseId}", response.Count, courseId);
        
        return Ok(response);
    }
    
    // GET: api/reviews/course/{courseId}/rating
    [HttpGet("course/{courseId}/rating")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseRating(int courseId)
    {
        _logger.LogInformation("Getting rating for course {CourseId}", courseId);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found for rating: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        var cacheKey = $"course_rating_{courseId}";
        var cachedRating = _cacheService.Get<CourseRatingDto>(cacheKey);
        
        if (cachedRating != null)
        {
            _logger.LogDebug("Returning cached rating for course {CourseId}", courseId);
            return Ok(cachedRating);
        }
        
        var avgRating = await _reviewRepository.GetAverageRatingAsync(courseId);
        var totalReviews = await _reviewRepository.GetReviewsCountAsync(courseId);
        var distribution = await _reviewRepository.GetRatingDistributionAsync(courseId);
        
        var response = new CourseRatingDto
        {
            CourseId = courseId,
            CourseTitle = course.Title,
            AverageRating = Math.Round(avgRating, 2),
            TotalReviews = totalReviews,
            RatingDistribution = distribution
        };
        
        _cacheService.Set(cacheKey, response, TimeSpan.FromMinutes(10));
        
        _logger.LogInformation("Course {CourseId} rating: {AvgRating} from {TotalReviews} reviews", 
            courseId, response.AverageRating, totalReviews);
        
        return Ok(response);
    }
    
    // GET: api/reviews/my
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetMyReviews - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting reviews for user {UserId}", userId);
        
        var reviews = await _reviewRepository.GetByUserIdAsync(userId);
        var response = reviews.Select(r => new ReviewResponseDto
        {
            ReviewId = r.ReviewId,
            UserId = r.UserId,
            UserName = r.User?.FullName ?? "Unknown",
            CourseId = r.CourseId,
            CourseTitle = r.Course?.Title ?? "Unknown",
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewDate = r.ReviewDate,
            IsApproved = r.IsApproved
        });
        
        _logger.LogInformation("Found {Count} reviews for user {UserId}", response.Count(), userId);
        
        return Ok(response);
    }
    
    // POST: api/reviews/course/{courseId}
    [HttpPost("course/{courseId}")]
    [Authorize]
    public async Task<IActionResult> CreateReview(int courseId, [FromBody] CreateReviewDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("CreateReview - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} creating review for course {CourseId}, Rating: {Rating}", 
            userId, courseId, createDto.Rating);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found for review: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        // Check if user already reviewed
        var existingReview = await _reviewRepository.GetByUserAndCourseAsync(userId, courseId);
        if (existingReview != null)
        {
            _logger.LogWarning("User {UserId} already reviewed course {CourseId}", userId, courseId);
            return BadRequest(new { message = "You have already reviewed this course" });
        }
        
        var review = new Review
        {
            UserId = userId,
            CourseId = courseId,
            Rating = createDto.Rating,
            Comment = createDto.Comment,
            IsApproved = false
        };
        
        await _reviewRepository.CreateAsync(review);
        
        _logger.LogInformation("Review created for course {CourseId} by user {UserId}, waiting for moderation", courseId, userId);
        
        // Clear cache
        _cacheService.Remove($"course_reviews_{courseId}_false");
        _cacheService.Remove($"course_reviews_{courseId}_true");
        _cacheService.Remove($"course_rating_{courseId}");
        _cacheService.Remove($"course_{courseId}");
        _cacheService.RemoveByPrefix("courses_filtered_");
        
        return Ok(new { message = "Review submitted successfully. Waiting for moderation." });
    }
    
    // PUT: api/reviews/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateReview - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} updating review {ReviewId}", userId, id);
        
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            _logger.LogWarning("Review not found: {ReviewId}", id);
            return NotFound(new { message = "Review not found" });
        }
        
        // Only author or admin can update
        if (review.UserId != userId && userRole != "admin")
        {
            _logger.LogWarning("User {UserId} not authorized to update review {ReviewId}", userId, id);
            return Forbid();
        }
        
        if (updateDto.Rating.HasValue)
        {
            review.Rating = updateDto.Rating.Value;
        }
        
        if (updateDto.Comment != null)
        {
            review.Comment = updateDto.Comment;
        }
        
        review.IsApproved = false;
        
        await _reviewRepository.UpdateAsync(review);
        
        _logger.LogInformation("Review {ReviewId} updated by user {UserId}", id, userId);
        
        // Clear cache
        _cacheService.Remove($"course_reviews_{review.CourseId}_false");
        _cacheService.Remove($"course_reviews_{review.CourseId}_true");
        _cacheService.Remove($"course_rating_{review.CourseId}");
        _cacheService.Remove($"course_{review.CourseId}");
        
        return Ok(new { message = "Review updated successfully" });
    }
    
    // DELETE: api/reviews/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("DeleteReview - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} deleting review {ReviewId}", userId, id);
        
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            _logger.LogWarning("Review not found: {ReviewId}", id);
            return NotFound(new { message = "Review not found" });
        }
        
        // Only author or admin can delete
        if (review.UserId != userId && userRole != "admin")
        {
            _logger.LogWarning("User {UserId} not authorized to delete review {ReviewId}", userId, id);
            return Forbid();
        }
        
        var courseId = review.CourseId;
        await _reviewRepository.DeleteAsync(review);
        
        _logger.LogInformation("Review {ReviewId} deleted by user {UserId}", id, userId);
        
        // Clear cache
        _cacheService.Remove($"course_reviews_{courseId}_false");
        _cacheService.Remove($"course_reviews_{courseId}_true");
        _cacheService.Remove($"course_rating_{courseId}");
        _cacheService.Remove($"course_{courseId}");
        _cacheService.RemoveByPrefix("courses_filtered_");
        
        return Ok(new { message = "Review deleted successfully" });
    }
    
    // PUT: api/reviews/{id}/approve
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ApproveReview(int id, [FromQuery] bool approve = true)
    {
        _logger.LogInformation("Admin approving/rejecting review {ReviewId}, Approve={Approve}", id, approve);
        
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            _logger.LogWarning("Review not found: {ReviewId}", id);
            return NotFound(new { message = "Review not found" });
        }
        
        review.IsApproved = approve;
        await _reviewRepository.UpdateAsync(review);
        
        _logger.LogInformation("Review {ReviewId} set to IsApproved={Approve}", id, approve);
        
        // Clear cache
        _cacheService.Remove($"course_reviews_{review.CourseId}_false");
        _cacheService.Remove($"course_reviews_{review.CourseId}_true");
        _cacheService.Remove($"course_rating_{review.CourseId}");
        _cacheService.Remove($"course_{review.CourseId}");
        
        return Ok(new { message = approve ? "Review approved" : "Review rejected" });
    }
    
    private ReviewResponseDto MapToResponseDto(Review review)
    {
        return new ReviewResponseDto
        {
            ReviewId = review.ReviewId,
            UserId = review.UserId,
            UserName = review.User?.FullName ?? "Unknown",
            UserAvatar = review.User?.AvatarUrl,
            CourseId = review.CourseId,
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.ReviewDate,
            IsApproved = review.IsApproved
        };
    }
}