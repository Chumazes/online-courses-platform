using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/courses/{courseId}/sections")]
[Authorize]
public class SectionsController : ControllerBase
{
    private readonly ISectionRepository _sectionRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<SectionsController> _logger;
    
    public SectionsController(
        ISectionRepository sectionRepository,
        ICourseRepository courseRepository,
        ILogger<SectionsController> logger)
    {
        _sectionRepository = sectionRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }
    
    // GET: api/courses/{courseId}/sections
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetSectionsByCourseId(int courseId)
    {
        _logger.LogInformation("Getting sections for course: {CourseId}", courseId);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found for sections: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        var sections = await _sectionRepository.GetByCourseIdAsync(courseId);
        
        var response = new List<SectionResponseDto>();
        foreach (var section in sections)
        {
            var lessonsCount = await _sectionRepository.GetLessonsCountAsync(section.SectionId);
            response.Add(new SectionResponseDto
            {
                SectionId = section.SectionId,
                CourseId = section.CourseId,
                CourseTitle = course.Title,
                Title = section.Title,
                Description = section.Description,
                SectionOrder = section.SectionOrder,
                LessonsCount = lessonsCount,
                CreatedAt = section.CreatedAt
            });
        }
        
        _logger.LogInformation("Found {Count} sections for course {CourseId}", response.Count, courseId);
        
        return Ok(response);
    }
    
    // GET: api/courses/{courseId}/sections/{sectionId}
    [HttpGet("{sectionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSectionById(int courseId, int sectionId)
    {
        _logger.LogInformation("Getting section {SectionId} for course {CourseId}", sectionId, courseId);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            _logger.LogWarning("Section {SectionId} not found in course {CourseId}", sectionId, courseId);
            return NotFound(new { message = "Section not found" });
        }
        
        var lessonsCount = await _sectionRepository.GetLessonsCountAsync(section.SectionId);
        
        var response = new SectionResponseDto
        {
            SectionId = section.SectionId,
            CourseId = section.CourseId,
            CourseTitle = course.Title,
            Title = section.Title,
            Description = section.Description,
            SectionOrder = section.SectionOrder,
            LessonsCount = lessonsCount,
            CreatedAt = section.CreatedAt
        };
        
        return Ok(response);
    }
    
    // POST: api/courses/{courseId}/sections
    [HttpPost]
    public async Task<IActionResult> CreateSection(int courseId, [FromBody] CreateSectionDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("CreateSection - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} creating section in course {CourseId}: {Title}", userId, courseId, createDto.Title);
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            _logger.LogWarning("Course not found for section creation: {CourseId}", courseId);
            return NotFound(new { message = "Course not found" });
        }
        
        // Only author or admin can add sections
        if (course.AuthorId != userId && userRole != "admin")
        {
            _logger.LogWarning("User {UserId} not authorized to create section in course {CourseId}", userId, courseId);
            return Forbid();
        }
        
        var section = new Section
        {
            CourseId = courseId,
            Title = createDto.Title,
            Description = createDto.Description,
            SectionOrder = createDto.SectionOrder
        };
        
        var created = await _sectionRepository.CreateAsync(section);
        
        _logger.LogInformation("Section created successfully: {SectionId} in course {CourseId}", created.SectionId, courseId);
        
        return CreatedAtAction(nameof(GetSectionById), 
            new { courseId = courseId, sectionId = created.SectionId }, 
            created);
    }
    
    // PUT: api/courses/{courseId}/sections/{sectionId}
    [HttpPut("{sectionId}")]
    public async Task<IActionResult> UpdateSection(int courseId, int sectionId, [FromBody] UpdateSectionDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateSection - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} updating section {SectionId} in course {CourseId}", userId, sectionId, courseId);
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            _logger.LogWarning("Section {SectionId} not found in course {CourseId}", sectionId, courseId);
            return NotFound(new { message = "Section not found" });
        }
        
        // Check authorization
        if (!await _sectionRepository.IsAuthorizedAsync(sectionId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to update section {SectionId}", userId, sectionId);
            return Forbid();
        }
        
        section.Title = updateDto.Title;
        section.Description = updateDto.Description;
        section.SectionOrder = updateDto.SectionOrder;
        
        await _sectionRepository.UpdateAsync(section);
        
        _logger.LogInformation("Section {SectionId} updated successfully", sectionId);
        
        return Ok(new { message = "Section updated successfully" });
    }
    
    // DELETE: api/courses/{courseId}/sections/{sectionId}
    [HttpDelete("{sectionId}")]
    public async Task<IActionResult> DeleteSection(int courseId, int sectionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("DeleteSection - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} deleting section {SectionId} from course {CourseId}", userId, sectionId, courseId);
        
        var section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            _logger.LogWarning("Section {SectionId} not found in course {CourseId}", sectionId, courseId);
            return NotFound(new { message = "Section not found" });
        }
        
        // Check authorization
        if (!await _sectionRepository.IsAuthorizedAsync(sectionId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to delete section {SectionId}", userId, sectionId);
            return Forbid();
        }
        
        await _sectionRepository.DeleteAsync(section);
        
        _logger.LogInformation("Section {SectionId} deleted successfully", sectionId);
        
        return Ok(new { message = "Section deleted successfully" });
    }
}