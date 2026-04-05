namespace OnlineCourses.Models.DTOs;

public class FileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class FileResponseDto
{
    public int ResourceId { get; set; }
    public int LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class AvatarUploadDto
{
    public string AvatarUrl { get; set; } = string.Empty;
}