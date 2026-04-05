using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
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
    private readonly ILogger<FilesController> _logger;
    
    public FilesController(
        IFileService fileService,
        IUserRepository userRepository,
        ILessonRepository lessonRepository,
        IConfiguration configuration,
        ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _userRepository = userRepository;
        _lessonRepository = lessonRepository;
        _configuration = configuration;
        _logger = logger;
    }
    
    // POST: api/files/avatar
    [Authorize]
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UploadAvatar - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} uploading avatar. File: {FileName}, Size: {Size} bytes", 
            userId, file.FileName, file.Length);
        
        var allowedExtensions = _configuration.GetSection("FileSettings:AllowedAvatarExtensions").Get<string[]>() 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var maxSizeMB = _configuration.GetValue<int>("FileSettings:MaxAvatarSizeMB", 5);
        
        if (!_fileService.IsValidFile(file, allowedExtensions, maxSizeMB))
        {
            _logger.LogWarning("Invalid avatar file for user {UserId}. File: {FileName}, Size: {Size}", 
                userId, file.FileName, file.Length);
            return BadRequest(new { message = $"Invalid file. Allowed: {string.Join(", ", allowedExtensions)}, Max size: {maxSizeMB}MB" });
        }
        
        var filePath = await _fileService.SaveFileAsync(file, "avatars");
        
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user != null)
        {
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                await _fileService.DeleteFileAsync(user.AvatarUrl);
            }
            
            user.AvatarUrl = filePath;
            await _userRepository.UpdateUserAsync(user);
        }
        
        _logger.LogInformation("Avatar uploaded successfully for user {UserId}. Path: {FilePath}", userId, filePath);
        
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
            _logger.LogWarning("UploadLessonFile - unauthorized access attempt");
            return Unauthorized();
        }
        
        _logger.LogInformation("User {UserId} uploading file to lesson {LessonId}. File: {FileName}, Size: {Size} bytes, Title: {Title}", 
            userId, lessonId, file.FileName, file.Length, title);
        
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null)
        {
            _logger.LogWarning("Lesson not found: {LessonId} for file upload by user {UserId}", lessonId, userId);
            return NotFound(new { message = "Lesson not found" });
        }
        
        // Check authorization
        if (!await _lessonRepository.IsAuthorizedAsync(lessonId, userId, userRole ?? ""))
        {
            _logger.LogWarning("User {UserId} not authorized to upload file to lesson {LessonId}", userId, lessonId);
            return Forbid();
        }
        
        var allowedExtensions = _configuration.GetSection("FileSettings:AllowedLessonExtensions").Get<string[]>() 
            ?? new[] { ".pdf", ".doc", ".docx", ".zip", ".rar", ".mp4", ".txt", ".pptx", ".xlsx" };
        var maxSizeMB = _configuration.GetValue<int>("FileSettings:MaxLessonFileSizeMB", 50);
        
        if (!_fileService.IsValidFile(file, allowedExtensions, maxSizeMB))
        {
            _logger.LogWarning("Invalid lesson file for user {UserId}. File: {FileName}, Size: {Size}", 
                userId, file.FileName, file.Length);
            return BadRequest(new { message = $"Invalid file. Allowed: {string.Join(", ", allowedExtensions)}, Max size: {maxSizeMB}MB" });
        }
        
        var filePath = await _fileService.SaveFileAsync(file, "lesson-files");
        
        _logger.LogInformation("File uploaded successfully to lesson {LessonId} by user {UserId}. Path: {FilePath}", 
            lessonId, userId, filePath);
        
        return Ok(new { 
            fileUrl = filePath, 
            fileName = file.FileName, 
            fileSize = file.Length, 
            message = "File uploaded successfully" 
        });
    }
    
    // GET: api/files/download
    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileUrl)
    {
        _logger.LogInformation("Downloading file: {FileUrl}", fileUrl);
        
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));
        
        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {FilePath}", fullPath);
            return NotFound(new { message = "File not found" });
        }
        
        var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        var fileName = Path.GetFileName(fullPath);
        var mimeType = GetMimeType(fileName);
        
        _logger.LogInformation("File downloaded successfully: {FileName}, Size: {Size} bytes", fileName, fileBytes.Length);
        
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
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}