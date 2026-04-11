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
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CoursesController> _logger;
    
    public CoursesController(
        ICourseRepository courseRepository,
        ICacheService cacheService,
        ILogger<CoursesController> logger)
    {
        _courseRepository = courseRepository;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    // GET: api/courses?page=1&pageSize=10&level=beginner&categoryId=1&search=python&minPrice=0&maxPrice=10000&sortBy=rating&sortOrder=desc
    [HttpGet]
    public async Task<IActionResult> GetAllCourses([FromQuery] CourseFilterParams filter)
    {
        _logger.LogInformation("Getting all courses with filters: Page={Page}, PageSize={PageSize}, Level={Level}, CategoryId={CategoryId}, Search={Search}, MinPrice={MinPrice}, MaxPrice={MaxPrice}, SortBy={SortBy}, SortOrder={SortOrder}, All={All}", 
            filter.PageNumber, filter.PageSize, filter.Level, filter.CategoryId, filter.Search, filter.MinPrice, filter.MaxPrice, filter.SortBy, filter.SortOrder, filter.All);
        
        var cacheKey = $"courses_filtered_{filter.PageNumber}_{filter.PageSize}_{filter.Level}_{filter.CategoryId}_{filter.Search}_{filter.MinPrice}_{filter.MaxPrice}_{filter.SortBy}_{filter.SortOrder}_{filter.All}";
        var cachedResult = _cacheService.Get<PaginatedResponse<CourseResponseDto>>(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Returning cached courses result for key: {CacheKey}", cacheKey);
            return Ok(cachedResult);
        }
        
        var (courses, totalCount) = await _courseRepository.GetFilteredAsync(filter);
        
        _logger.LogInformation("Found {TotalCount} courses matching filters", totalCount);
        
        var items = new List<CourseResponseDto>();
        foreach (var course in courses)
        {
            var studentCount = await _courseRepository.GetStudentsCountAsync(course.CourseId);
            items.Add(MapToResponseDto(course, studentCount));
        }
        
        var response = new PaginatedResponse<CourseResponseDto>
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
            Items = items
        };
        
        _cacheService.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return Ok(response);
    }
    
    // GET: api/courses/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        _logger.LogInformation("Getting course by ID: {CourseId}", id);
        
        var cacheKey = $"course_{id}";
        var cachedCourse = _cacheService.Get<CourseResponseDto>(cacheKey);
        
        if (cachedCourse != null)
        {
            _logger.LogDebug("Returning cached course {CourseId}", id);
            return Ok(cachedCourse);
        }
        
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            _logger.LogWarning("Course not found: {CourseId}", id);
            return NotFound(new { message = "Course not found" });
        }
        
        var studentCount = await _courseRepository.GetStudentsCountAsync(course.CourseId);
        var response = MapToResponseDto(course, studentCount);
        
        _cacheService.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return Ok(response);
    }
    
    // GET: api/courses/my
    [Authorize(Roles = "teacher,admin")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetMyCourses - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("Getting courses for author: {UserId}", userId);
        
        var cacheKey = $"my_courses_{userId}";
        var cachedCourses = _cacheService.Get<List<CourseResponseDto>>(cacheKey);
        
        if (cachedCourses != null)
        {
            _logger.LogDebug("Returning cached my courses for user {UserId}", userId);
            return Ok(cachedCourses);
        }
        
        var courses = await _courseRepository.GetByAuthorIdAsync(userId);
        
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
                CategoryId = course.CategoryId,
                CategoryName = course.Category?.Name,
                AuthorId = course.AuthorId,
                AuthorName = string.Empty,
                CoverImageUrl = course.CoverImageUrl,
                CreatedAt = course.CreatedAt,
                TotalStudents = studentCount
            });
        }
        
        _logger.LogInformation("Found {Count} courses for author {UserId}", response.Count, userId);
        
        _cacheService.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return Ok(response);
    }
    
    // GET: api/courses/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        _logger.LogInformation("Getting all categories");
        
        var cacheKey = "categories_list";
        var cachedCategories = _cacheService.Get<List<object>>(cacheKey);
        
        if (cachedCategories != null)
        {
            _logger.LogDebug("Returning cached categories");
            return Ok(cachedCategories);
        }
        
        var categories = await _courseRepository.GetCategoriesAsync();
        _cacheService.Set(cacheKey, categories, TimeSpan.FromMinutes(30));
        
        _logger.LogInformation("Found {Count} categories", categories.Count());
        
        return Ok(categories);
    }
    
    // POST: api/courses
    [Authorize(Roles = "teacher,admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("CreateCourse - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} creating new course: {Title}, Price: {Price}, Level: {Level}", 
            userId, createDto.Title, createDto.Price, createDto.Level);
        
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
        
        _logger.LogInformation("Course created successfully: {CourseId}, Title: {Title}, Author: {UserId}", 
            created.CourseId, created.Title, userId);
        
        // Очищаем кэш списков курсов
        _cacheService.RemoveByPrefix("courses_filtered_");
        _cacheService.RemoveByPrefix("courses_all_");
        _cacheService.RemoveByPrefix("my_courses_");
        
        return CreatedAtAction(nameof(GetCourseById), new { id = created.CourseId }, created);
    }
    
    // PUT: api/courses/{id}
    [Authorize(Roles = "teacher,admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateCourse - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} updating course {CourseId}", userId, id);
        
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            _logger.LogWarning("UpdateCourse - course not found: {CourseId}", id);
            return NotFound(new { message = "Course not found" });
        }
        
        // Only author or admin can update
        if (course.AuthorId != userId && userRole != "admin")
        {
            _logger.LogWarning("User {UserId} attempted to update course {CourseId} without permission", userId, id);
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
        
        _logger.LogInformation("Course updated successfully: {CourseId}, Title: {Title}, Status: {Status}", 
            id, course.Title, course.Status);
        
        // Очищаем кэш
        _cacheService.Remove($"course_{id}");
        _cacheService.RemoveByPrefix("courses_filtered_");
        _cacheService.RemoveByPrefix("courses_all_");
        _cacheService.RemoveByPrefix("my_courses_");
        
        return Ok(new { message = "Course updated successfully" });
    }
    
    // DELETE: api/courses/{id}
    [Authorize(Roles = "teacher,admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("DeleteCourse - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} deleting course {CourseId}", userId, id);
        
        var course = await _courseRepository.GetByIdAsync(id);
        
        if (course == null)
        {
            _logger.LogWarning("DeleteCourse - course not found: {CourseId}", id);
            return NotFound(new { message = "Course not found" });
        }
        
        // Only author or admin can delete
        if (course.AuthorId != userId && userRole != "admin")
        {
            _logger.LogWarning("User {UserId} attempted to delete course {CourseId} without permission", userId, id);
            return Forbid();
        }
        
        await _courseRepository.DeleteAsync(course);
        
        _logger.LogInformation("Course deleted successfully: {CourseId}, Title: {Title}", id, course.Title);
        
        // Очищаем кэш
        _cacheService.Remove($"course_{id}");
        _cacheService.RemoveByPrefix("courses_filtered_");
        _cacheService.RemoveByPrefix("courses_all_");
        _cacheService.RemoveByPrefix("my_courses_");
        
        return Ok(new { message = "Course deleted successfully" });
    }
    
    // Вспомогательный метод для маппинга
    private CourseResponseDto MapToResponseDto(Course course, int studentCount)
    {
        return new CourseResponseDto
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
    }
}
