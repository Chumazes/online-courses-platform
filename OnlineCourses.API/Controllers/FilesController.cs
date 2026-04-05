using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IUserRepository _userRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IConfiguration _configuration;

    public FilesController(
        IFileService fileService,
        IUserRepository userRepository,
        ILessonRepository lessonRepository,
        IConfiguration configuration)
    {
        _fileService = fileService;
        _userRepository = userRepository;
        _lessonRepository = lessonRepository;
        _configuration = configuration;
    }

    // POST: api/files/avatar
    [Authorize]
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var allowedExtensions = _configuration.GetSection("FileSettings:AllowedAvatarExtensions").Get<string[]>() 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var maxSizeMB = _configuration.GetValue<int>("FileSettings:MaxAvatarSizeMB", 5);

        if (!_fileService.IsValidFile(file, allowedExtensions, maxSizeMB))
        {
            return BadRequest(new { message = $"Invalid file. Allowed: {string.Join(", ", allowedExtensions)}, Max size: {maxSizeMB}MB" });
        }

        var filePath = await _fileService.SaveFileAsync(file, "avatars");
        
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user != null)
        {
            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await _fileService.DeleteFileAsync(user.AvatarUrl);
            }
            
            user.AvatarUrl = filePath;
            await _userRepository.UpdateUserAsync(user);
        }

        return Ok(new { avatarUrl = filePath, message = "Avatar uploaded successfully" });
    }

    // POST: api/files/lesson/{lessonId}
    [Authorize(Roles = "teacher,admin")]
    [HttpPost("lesson/{lessonId}")]
    public async Task<IActionResult> UploadLessonFile(int lessonId, IFormFile file, [FromForm] string title)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }

        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            return Forbid();
        }

        var allowedExtensions = _configuration.GetSection("FileSettings:AllowedLessonExtensions").Get<string[]>() 
            ?? new[] { ".pdf", ".doc", ".docx", ".zip", ".rar", ".mp4", ".txt" };
        var maxSizeMB = _configuration.GetValue<int>("FileSettings:MaxLessonFileSizeMB", 50);

        if (!_fileService.IsValidFile(file, allowedExtensions, maxSizeMB))
        {
            return BadRequest(new { message = $"Invalid file. Allowed: {string.Join(", ", allowedExtensions)}, Max size: {maxSizeMB}MB" });
        }

        var filePath = await _fileService.SaveFileAsync(file, "lesson-files");

        // Здесь нужно добавить сохранение в таблицу Resources
        // Это требует создания ResourceRepository и ResourceController
        // Пока возвращаем путь к файлу

        return Ok(new { fileUrl = filePath, fileName = file.FileName, fileSize = file.Length, message = "File uploaded successfully" });
    }

    // GET: api/files/download
    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileUrl)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));
        
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound(new { message = "File not found" });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        var fileName = Path.GetFileName(fullPath);
        var mimeType = GetMimeType(fileName);

        return File(fileBytes, mimeType, fileName);
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".mp4" => "video/mp4",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}