using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<EnrollmentsController> _logger;
    
    public EnrollmentsController(
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        ILogger<EnrollmentsController> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }
    
    // GET: api/enrollments/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetMyEnrollments - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting enrollments for user: {UserId}", userId);
        
        var enrollments = await _enrollmentRepository.GetByUserIdAsync(userId);
        
        var response = enrollments.Select(e => new EnrollmentResponseDto
        {
            EnrollmentId = e.EnrollmentId,
            CourseId = e.CourseId,
            CourseTitle = e.Course?.Title ?? "Unknown",
            EnrollmentDate = e.EnrollmentDate,
            Status = e.Status,
            OverallProgress = e.OverallProgress,
            CompletedAt = e.CompletedAt
        });
        
        _logger.LogInformation("Found {Count} enrollments for user {UserId}", response.Count(), userId);
        
        return Ok(response);
    }
    
    // POST: api/enrollments
    [HttpPost]
    public async Task<IActionResult> EnrollInCourse([FromBody] EnrollmentRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("EnrollInCourse - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} attempting to enroll in course {CourseId}", userId, request.CourseId);
        
        // Check if course exists
        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            _logger.LogWarning("EnrollInCourse - course not found: {CourseId}", request.CourseId);
            return NotFound(new { message = "Course not found" });
        }
        
        // Check if already enrolled
        var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(userId, request.CourseId);
        if (isEnrolled)
        {
            _logger.LogWarning("User {UserId} already enrolled in course {CourseId}", userId, request.CourseId);
            return BadRequest(new { message = "Already enrolled in this course" });
        }
        
        var enrollment = new OnlineCourses.Models.Entities.Enrollment
        {
            UserId = userId,
            CourseId = request.CourseId,
            Status = "active",
            OverallProgress = 0
        };
        
        var created = await _enrollmentRepository.CreateAsync(enrollment);
        
        _logger.LogInformation("User {UserId} successfully enrolled in course {CourseId}, EnrollmentId: {EnrollmentId}", 
            userId, request.CourseId, created.EnrollmentId);
        
        return Ok(new EnrollmentResponseDto
        {
            EnrollmentId = created.EnrollmentId,
            CourseId = created.CourseId,
            CourseTitle = course.Title,
            EnrollmentDate = created.EnrollmentDate,
            Status = created.Status,
            OverallProgress = created.OverallProgress
        });
    }
    
    // DELETE: api/enrollments/{courseId}
    [HttpDelete("{courseId}")]
    public async Task<IActionResult> UnenrollFromCourse(int courseId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UnenrollFromCourse - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} attempting to unenroll from course {CourseId}", userId, courseId);
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null)
        {
            _logger.LogWarning("UnenrollFromCourse - enrollment not found for user {UserId}, course {CourseId}", userId, courseId);
            return NotFound(new { message = "Enrollment not found" });
        }
        
        enrollment.Status = "expired";
        await _enrollmentRepository.UpdateAsync(enrollment);
        
        _logger.LogInformation("User {UserId} successfully unenrolled from course {CourseId}", userId, courseId);
        
        return Ok(new { message = "Successfully unenrolled from course" });
    }
    
    // GET: api/enrollments/course/{courseId}
    [Authorize(Roles = "teacher,admin")]
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseEnrollments(int courseId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetCourseEnrollments - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting enrollments for course {CourseId} by user {UserId}", courseId, userId);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("GetCourseEnrollments - course not found: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
        
        var response = enrollments.Select(e => new
        {
            e.EnrollmentId,
            e.UserId,
            UserName = e.User?.FullName,
            e.EnrollmentDate,
            e.Status,
            e.OverallProgress
        });
        
        _logger.LogInformation("Found {Count} enrollments for course {CourseId}", response.Count(), courseId);
        
        return Ok(response);
    }
}