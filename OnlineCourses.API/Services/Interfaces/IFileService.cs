namespace OnlineCourses.API.Services.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder);
    Task<bool> DeleteFileAsync(string filePath);
    string GetFileUrl(string fileName, string subFolder);
    bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxSizeMB);
}