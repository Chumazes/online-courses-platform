using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    
    public EnrollmentsController(
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
    }
    
    // GET: api/enrollments/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
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
        
        return Ok(response);
    }
    
    // POST: api/enrollments
    [HttpPost]
    public async Task<IActionResult> EnrollInCourse([FromBody] EnrollmentRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        // Check if course exists
        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }
        
        // Check if already enrolled
        var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(userId, request.CourseId);
        if (isEnrolled)
        {
            return BadRequest(new { message = "Already enrolled in this course" });
        }
        
        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = request.CourseId,
            Status = "active",
            OverallProgress = 0
        };
        
        var created = await _enrollmentRepository.CreateAsync(enrollment);
        
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
            return Unauthorized();
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null)
        {
            return NotFound(new { message = "Enrollment not found" });
        }
        
        enrollment.Status = "expired";
        await _enrollmentRepository.UpdateAsync(enrollment);
        
        return Ok(new { message = "Successfully unenrolled from course" });
    }
    
    // GET: api/enrollments/course/{courseId}
    [Authorize(Roles = "teacher,admin")]
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseEnrollments(int courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
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
        
        return Ok(response);
    }
}