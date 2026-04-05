using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _courseRepository;
    
    public CoursesController(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }
    
    // GET: api/courses
    [HttpGet]
    public async Task<IActionResult> GetAllCourses([FromQuery] bool all = false)
    {
        var courses = await _courseRepository.GetAllAsync(all);
        
        var response = new List<CourseResponseDto>();
        foreach (var course in courses)
        {
            var studentCount = await _courseRepository.GetStudentsCountAsync(course.CourseId);
            response.Add(new CourseResponseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                Price = course.Price,
                Level = course.Level,
                Status = course.Status,
                CoverImageUrl = course.CoverImageUrl,
                AvgRating = course.AvgRating,
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                AuthorId = course.AuthorId,
                AuthorName = course.Author?.FullName ?? "Unknown",
                CreatedAt = course.CreatedAt,
                TotalStudents = studentCount
            });
        }
        
        return Ok(response);
    }
    
    // GET: api/courses/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }
        
        var studentCount = await _courseRepository.GetStudentsCountAsync(course.CourseId);
        var response = new CourseResponseDto
        {
            CourseId = course.CourseId,
            Title = course.Title,
            Description = course.Description,
            Price = course.Price,
            Level = course.Level,
            Status = course.Status,
            CoverImageUrl = course.CoverImageUrl,
            AvgRating = course.AvgRating,
            CategoryId = course.CategoryId,
            CategoryName = course.Category?.Name,
            AuthorId = course.AuthorId,
            AuthorName = course.Author?.FullName ?? "Unknown",
            CreatedAt = course.CreatedAt,
            TotalStudents = studentCount
        };
        
        return Ok(response);
    }
    
    // POST: api/courses
    [Authorize(Roles = "teacher,admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var course = new Course
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Price = createDto.Price,
            Level = createDto.Level,
            CategoryId = createDto.CategoryId,
            CoverImageUrl = createDto.CoverImageUrl,
            AuthorId = userId,
            Status = "draft"
        };
        
        var created = await _courseRepository.CreateAsync(course);
        
        return CreatedAtAction(nameof(GetCourseById), new { id = created.CourseId }, created);
    }
    
    // PUT: api/courses/{id}
    [Authorize(Roles = "teacher,admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto updateDto)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        // Only author or admin can update
        if (course.AuthorId != userId && userRole != "admin")
        {
            return Forbid();
        }
        
        course.Title = updateDto.Title;
        course.Description = updateDto.Description;
        course.Price = updateDto.Price;
        course.Level = updateDto.Level;
        course.Status = updateDto.Status;
        course.CategoryId = updateDto.CategoryId;
        course.CoverImageUrl = updateDto.CoverImageUrl;
        
        await _courseRepository.UpdateAsync(course);
        
        return Ok(new { message = "Course updated successfully" });
    }
    
    // DELETE: api/courses/{id}
    [Authorize(Roles = "teacher,admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        // Only author or admin can delete
        if (course.AuthorId != userId && userRole != "admin")
        {
            return Forbid();
        }
        
        await _courseRepository.DeleteAsync(course);
        
        return Ok(new { message = "Course deleted successfully" });
    }
    
    // GET: api/courses/my
    [Authorize(Roles = "teacher,admin")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var courses = await _courseRepository.GetByAuthorIdAsync(userId);
        
        var response = courses.Select(course => new CourseResponseDto
        {
            CourseId = course.CourseId,
            Title = course.Title,
            Description = course.Description,
            Price = course.Price,
            Level = course.Level,
            Status = course.Status,
            CoverImageUrl = course.CoverImageUrl,
            CreatedAt = course.CreatedAt
        });
        
        return Ok(response);
    }
}