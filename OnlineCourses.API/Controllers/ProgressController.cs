using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IProgressRepository _progressRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<ProgressController> _logger;
    
    public ProgressController(
        IProgressRepository progressRepository,
        IEnrollmentRepository enrollmentRepository,
        ILessonRepository lessonRepository,
        ILogger<ProgressController> logger)
    {
        _progressRepository = progressRepository;
        _enrollmentRepository = enrollmentRepository;
        _lessonRepository = lessonRepository;
        _logger = logger;
    }
    
    // POST: api/progress/update
    [HttpPost("update")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateProgress - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} updating progress for lesson {LessonId}. Completed: {IsCompleted}, WatchTime: {WatchTime}s", 
            userId, updateDto.LessonId, updateDto.IsCompleted, updateDto.WatchTime);
        
        var lesson = await _lessonRepository.GetByIdAsync(updateDto.LessonId);
        if (lesson == null)
        {
            _logger.LogWarning("UpdateProgress - lesson not found: {LessonId}", updateDto.LessonId);
            return NotFound(new { message = "Lesson not found" });
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, lesson.Section.CourseId);
        if (enrollment == null)
        {
            _logger.LogWarning("User {UserId} not enrolled in course {CourseId} for lesson {LessonId}", 
                userId, lesson.Section.CourseId, updateDto.LessonId);
            return BadRequest(new { message = "You are not enrolled in this course" });
        }
        
        var progress = new OnlineCourses.Models.Entities.LessonProgress
        {
            EnrollmentId = enrollment.EnrollmentId,
            LessonId = updateDto.LessonId,
            IsCompleted = updateDto.IsCompleted,
            WatchTime = updateDto.WatchTime
        };
        
        await _progressRepository.CreateOrUpdateProgressAsync(progress);
        await _progressRepository.UpdateEnrollmentProgressAsync(enrollment.EnrollmentId);
        
        _logger.LogInformation("Progress updated successfully for user {UserId}, lesson {LessonId}", userId, updateDto.LessonId);
        
        return Ok(new { message = "Progress updated successfully" });
    }
    
    // GET: api/progress/course/{courseId}
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseProgress(int courseId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetCourseProgress - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting course progress for user {UserId}, course {CourseId}", userId, courseId);
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null)
        {
            _logger.LogWarning("User {UserId} not enrolled in course {CourseId}", userId, courseId);
            return BadRequest(new { message = "You are not enrolled in this course" });
        }
        
        var totalLessons = await _progressRepository.GetTotalLessonsCountAsync(courseId);
        var completedLessons = await _progressRepository.GetCompletedLessonsCountAsync(enrollment.EnrollmentId);
        
        var response = new CourseProgressResponseDto
        {
            CourseId = courseId,
            CourseTitle = enrollment.Course?.Title ?? "Unknown",
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons,
            OverallProgress = enrollment.OverallProgress,
            Status = enrollment.Status,
            CompletedAt = enrollment.CompletedAt
        };
        
        _logger.LogInformation("Course progress for user {UserId}, course {CourseId}: {Progress}% completed", 
            userId, courseId, response.OverallProgress);
        
        return Ok(response);
    }
    
    // GET: api/progress/lesson/{lessonId}
    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> GetLessonProgress(int lessonId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetLessonProgress - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting lesson progress for user {UserId}, lesson {LessonId}", userId, lessonId);
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null)
        {
            _logger.LogWarning("GetLessonProgress - lesson not found: {LessonId}", lessonId);
            return NotFound(new { message = "Lesson not found" });
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, lesson.Section.CourseId);
        if (enrollment == null)
        {
            _logger.LogWarning("User {UserId} not enrolled in course for lesson {LessonId}", userId, lessonId);
            return BadRequest(new { message = "You are not enrolled in this course" });
        }
        
        var progress = await _progressRepository.GetProgressAsync(enrollment.EnrollmentId, lessonId);
        
        if (progress == null)
        {
            return Ok(new LessonProgressResponseDto
            {
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                IsCompleted = false,
                WatchTime = 0
            });
        }
        
        return Ok(new LessonProgressResponseDto
        {
            ProgressId = progress.ProgressId,
            LessonId = progress.LessonId,
            LessonTitle = lesson.Title,
            IsCompleted = progress.IsCompleted,
            WatchTime = progress.WatchTime,
            LastAccessed = progress.LastAccessed,
            CompletedAt = progress.CompletedAt
        });
    }
    
    // GET: api/progress/my
    [HttpGet("my")]
    public async Task<IActionResult> GetAllMyProgress()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetAllMyProgress - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting all progress for user {UserId}", userId);
        
        var enrollments = await _enrollmentRepository.GetByUserIdAsync(userId);
        var result = new List<CourseProgressResponseDto>();
        
        foreach (var enrollment in enrollments)
        {
            var totalLessons = await _progressRepository.GetTotalLessonsCountAsync(enrollment.CourseId);
            var completedLessons = await _progressRepository.GetCompletedLessonsCountAsync(enrollment.EnrollmentId);
            
            result.Add(new CourseProgressResponseDto
            {
                CourseId = enrollment.CourseId,
                CourseTitle = enrollment.Course?.Title ?? "Unknown",
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                OverallProgress = enrollment.OverallProgress,
                Status = enrollment.Status,
                CompletedAt = enrollment.CompletedAt
            });
        }
        
        _logger.LogInformation("Found {Count} courses with progress for user {UserId}", result.Count, userId);
        
        return Ok(result);
    }
}