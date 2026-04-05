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
public class ProgressController : ControllerBase
{
    private readonly IProgressRepository _progressRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILessonRepository _lessonRepository;
    
    public ProgressController(
        IProgressRepository progressRepository,
        IEnrollmentRepository enrollmentRepository,
        ILessonRepository lessonRepository)
    {
        _progressRepository = progressRepository;
        _enrollmentRepository = enrollmentRepository;
        _lessonRepository = lessonRepository;
    }
    
    // POST: api/progress/update
    [HttpPost("update")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(updateDto.LessonId);
        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, lesson.Section.CourseId);
        if (enrollment == null)
        {
            return BadRequest(new { message = "You are not enrolled in this course" });
        }
        
        var progress = new LessonProgress
        {
            EnrollmentId = enrollment.EnrollmentId,
            LessonId = updateDto.LessonId,
            IsCompleted = updateDto.IsCompleted,
            WatchTime = updateDto.WatchTime
        };
        
        await _progressRepository.CreateOrUpdateProgressAsync(progress);
        await _progressRepository.UpdateEnrollmentProgressAsync(enrollment.EnrollmentId);
        
        return Ok(new { message = "Progress updated successfully" });
    }
    
    // GET: api/progress/course/{courseId}
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetCourseProgress(int courseId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null)
        {
            return BadRequest(new { message = "You are not enrolled in this course" });
        }
        
        var progressList = await _progressRepository.GetProgressByEnrollmentAsync(enrollment.EnrollmentId);
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
        
        return Ok(response);
    }
    
    // GET: api/progress/lesson/{lessonId}
    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> GetLessonProgress(int lessonId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }
        
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, lesson.Section.CourseId);
        if (enrollment == null)
        {
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
            return Unauthorized();
        }
        
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
        
        return Ok(result);
    }
}