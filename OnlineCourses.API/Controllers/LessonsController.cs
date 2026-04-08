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
    private readonly ILogger<LessonsController> _logger;
    
    public LessonsController(
        ILessonRepository lessonRepository,
        ISectionRepository sectionRepository,
        ILogger<LessonsController> logger)
    {
        _lessonRepository = lessonRepository;
        _sectionRepository = sectionRepository;
        _logger = logger;
    }
    
    // GET: api/sections/{sectionId}/lessons
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLessonsBySectionId(int sectionId)
    {
        _logger.LogInformation("Getting lessons for section: {SectionId}", sectionId);
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            _logger.LogWarning("Section not found: {SectionId}", sectionId);
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
            FileName = l.FileName,
            FileUrl = l.FileUrl,
            FileType = l.FileType,
            FileSize = l.FileSize,
            DurationMinutes = l.DurationMinutes,
            LessonOrder = l.LessonOrder,
            IsFree = l.IsFree,
            CreatedAt = l.CreatedAt
        });
        
        _logger.LogInformation("Found {Count} lessons for section {SectionId}", response.Count(), sectionId);
        
        return Ok(response);
    }
    
    // GET: api/sections/{sectionId}/lessons/{lessonId}
    [HttpGet("{lessonId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLessonById(int sectionId, int lessonId)
    {
        _logger.LogInformation("Getting lesson {LessonId} from section {SectionId}", lessonId, sectionId);
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            _logger.LogWarning("Section not found: {SectionId}", sectionId);
            return NotFound(new { message = "Section not found" });
        }
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            _logger.LogWarning("Lesson {LessonId} not found in section {SectionId}", lessonId, sectionId);
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
            FileName = lesson.FileName,
            FileUrl = lesson.FileUrl,
            FileType = lesson.FileType,
            FileSize = lesson.FileSize,
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
            _logger.LogWarning("CreateLesson - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} creating lesson in section {SectionId}: {Title}", userId, sectionId, createDto.Title);
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null)
        {
            _logger.LogWarning("Section not found for lesson creation: {SectionId}", sectionId);
            return NotFound(new { message = "Section not found" });
        }
        
        // Check authorization (course author or admin)
        if (!await _sectionRepository.IsAuthorizedAsync(sectionId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to create lesson in section {SectionId}", userId, sectionId);
            return Forbid();
        }

        var existingLessons = await _lessonRepository.GetBySectionIdAsync(sectionId);
        if (existingLessons.Any(item => item.LessonOrder == createDto.LessonOrder))
        {
            _logger.LogWarning("Duplicate lesson order {LessonOrder} in section {SectionId}", createDto.LessonOrder, sectionId);
            return BadRequest(new { message = "Урок с таким порядковым номером уже существует в этой секции." });
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
        
        _logger.LogInformation("Lesson created successfully: {LessonId} in section {SectionId}", created.LessonId, sectionId);

        var response = new LessonResponseDto
        {
            LessonId = created.LessonId,
            SectionId = created.SectionId,
            SectionTitle = section.Title,
            Title = created.Title,
            Content = created.Content,
            LessonType = created.LessonType,
            VideoUrl = created.VideoUrl,
            FileName = created.FileName,
            FileUrl = created.FileUrl,
            FileType = created.FileType,
            FileSize = created.FileSize,
            DurationMinutes = created.DurationMinutes,
            LessonOrder = created.LessonOrder,
            IsFree = created.IsFree,
            CreatedAt = created.CreatedAt
        };

        return CreatedAtAction(nameof(GetLessonById), 
            new { sectionId = sectionId, lessonId = created.LessonId }, 
            response);
    }
    
    // PUT: api/sections/{sectionId}/lessons/{lessonId}
    [HttpPut("{lessonId}")]
    public async Task<IActionResult> UpdateLesson(int sectionId, int lessonId, [FromBody] UpdateLessonDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateLesson - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} updating lesson {LessonId} in section {SectionId}", userId, lessonId, sectionId);
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            _logger.LogWarning("Lesson {LessonId} not found in section {SectionId}", lessonId, sectionId);
            return NotFound(new { message = "Lesson not found" });
        }
        
        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to update lesson {LessonId}", userId, lessonId);
            return Forbid();
        }

        var existingLessons = await _lessonRepository.GetBySectionIdAsync(sectionId);
        if (existingLessons.Any(item => item.LessonId != lessonId && item.LessonOrder == updateDto.LessonOrder))
        {
            _logger.LogWarning("Duplicate lesson order {LessonOrder} in section {SectionId} while updating lesson {LessonId}", updateDto.LessonOrder, sectionId, lessonId);
            return BadRequest(new { message = "Урок с таким порядковым номером уже существует в этой секции." });
        }
        
        lesson.Title = updateDto.Title;
        lesson.Content = updateDto.Content;
        lesson.LessonType = updateDto.LessonType;
        lesson.VideoUrl = updateDto.VideoUrl;
        lesson.DurationMinutes = updateDto.DurationMinutes;
        lesson.LessonOrder = updateDto.LessonOrder;
        lesson.IsFree = updateDto.IsFree;
        
        await _lessonRepository.UpdateAsync(lesson);
        
        _logger.LogInformation("Lesson {LessonId} updated successfully", lessonId);
        
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
            _logger.LogWarning("DeleteLesson - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} deleting lesson {LessonId} from section {SectionId}", userId, lessonId, sectionId);
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            _logger.LogWarning("Lesson {LessonId} not found in section {SectionId}", lessonId, sectionId);
            return NotFound(new { message = "Lesson not found" });
        }
        
        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to delete lesson {LessonId}", userId, lessonId);
            return Forbid();
        }
        
        await _lessonRepository.DeleteAsync(lesson);
        
        _logger.LogInformation("Lesson {LessonId} deleted successfully", lessonId);
        
        return Ok(new { message = "Lesson deleted successfully" });
    }
}
