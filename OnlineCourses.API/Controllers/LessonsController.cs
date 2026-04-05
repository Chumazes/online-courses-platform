using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/sections/{sectionId}/lessons")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ISectionRepository _sectionRepository;
    
    public LessonsController(
        ILessonRepository lessonRepository,
        ISectionRepository sectionRepository)
    {
        _lessonRepository = lessonRepository;
        _sectionRepository = sectionRepository;
    }
    
    // GET: api/sections/{sectionId}/lessons
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLessonsBySectionId(int sectionId)
    {
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            return NotFound(new { message = "Section not found" });
        }
        
        var lessons = await _lessonRepository.GetBySectionIdAsync(sectionId);
        
        var response = lessons.Select(l => new LessonResponseDto
        {
            LessonId = l.LessonId,
            SectionId = l.SectionId,
            SectionTitle = section.Title,
            Title = l.Title,
            Content = l.Content,
            LessonType = l.LessonType,
            VideoUrl = l.VideoUrl,
            DurationMinutes = l.DurationMinutes,
            LessonOrder = l.LessonOrder,
            IsFree = l.IsFree,
            CreatedAt = l.CreatedAt
        });
        
        return Ok(response);
    }
    
    // GET: api/sections/{sectionId}/lessons/{lessonId}
    [HttpGet("{lessonId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLessonById(int sectionId, int lessonId)
    {
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            return NotFound(new { message = "Section not found" });
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new { message = "Lesson not found" });
        }
        
        var response = new LessonResponseDto
        {
            LessonId = lesson.LessonId,
            SectionId = lesson.SectionId,
            SectionTitle = section.Title,
            Title = lesson.Title,
            Content = lesson.Content,
            LessonType = lesson.LessonType,
            VideoUrl = lesson.VideoUrl,
            DurationMinutes = lesson.DurationMinutes,
            LessonOrder = lesson.LessonOrder,
            IsFree = lesson.IsFree,
            CreatedAt = lesson.CreatedAt
        };
        
        return Ok(response);
    }
    
    // POST: api/sections/{sectionId}/lessons
    [HttpPost]
    public async Task<IActionResult> CreateLesson(int sectionId, [FromBody] CreateLessonDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            return NotFound(new { message = "Section not found" });
        }
        
        // Check authorization (course author or admin)
        if (!await _sectionRepository.IsAuthorizedAsync(sectionId, userId, userRole ?? ""))
        {
            return Forbid();
        }
        
        var lesson = new Lesson
        {
            SectionId = sectionId,
            Title = createDto.Title,
            Content = createDto.Content,
            LessonType = createDto.LessonType,
            VideoUrl = createDto.VideoUrl,
            DurationMinutes = createDto.DurationMinutes,
            LessonOrder = createDto.LessonOrder,
            IsFree = createDto.IsFree
        };
        
        var created = await _lessonRepository.CreateAsync(lesson);
        
        return CreatedAtAction(nameof(GetLessonById), 
            new { sectionId = sectionId, lessonId = created.LessonId }, 
            created);
    }
    
    // PUT: api/sections/{sectionId}/lessons/{lessonId}
    [HttpPut("{lessonId}")]
    public async Task<IActionResult> UpdateLesson(int sectionId, int lessonId, [FromBody] UpdateLessonDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new { message = "Lesson not found" });
        }
        
        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            return Forbid();
        }
        
        lesson.Title = updateDto.Title;
        lesson.Content = updateDto.Content;
        lesson.LessonType = updateDto.LessonType;
        lesson.VideoUrl = updateDto.VideoUrl;
        lesson.DurationMinutes = updateDto.DurationMinutes;
        lesson.LessonOrder = updateDto.LessonOrder;
        lesson.IsFree = updateDto.IsFree;
        
        await _lessonRepository.UpdateAsync(lesson);
        
        return Ok(new { message = "Lesson updated successfully" });
    }
    
    // DELETE: api/sections/{sectionId}/lessons/{lessonId}
    [HttpDelete("{lessonId}")]
    public async Task<IActionResult> DeleteLesson(int sectionId, int lessonId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new { message = "Lesson not found" });
        }
        
        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            return Forbid();
        }
        
        await _lessonRepository.DeleteAsync(lesson);
        
        return Ok(new { message = "Lesson deleted successfully" });
    }
}