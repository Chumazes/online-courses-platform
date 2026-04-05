using OnlineCourses.API.Services.Interfaces;

namespace OnlineCourses.API.Services.Implementations;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;

    public FileService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<FileService> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
    {
        try
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", subFolder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved: {FilePath}", filePath);
            
            return $"/uploads/{subFolder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", filePath.TrimStart('/'));
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted: {FilePath}", fullPath);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return false;
        }
    }

    public string GetFileUrl(string fileName, string subFolder)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5064";
        return $"{baseUrl}/uploads/{subFolder}/{fileName}";
    }

    public bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxSizeMB)
    {
        if (file == null || file.Length == 0)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return false;

        var maxSizeBytes = maxSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
            return false;

        return true;
    }
}