using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineCourses.API.Controllers;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Tests.Controllers;

public class FilesControllerTests
{
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILessonRepository> _lessonRepository = new();
    private readonly Mock<ILogger<FilesController>> _logger = new();

    [Fact]
    public async Task UploadAvatar_WhenFileIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var file = CreateFormFile("avatar.exe", "application/octet-stream");

        _fileService
            .Setup(service => service.IsValidFile(file, It.IsAny<string[]>(), It.IsAny<long>()))
            .Returns(false);

        // Act
        var result = await controller.UploadAvatar(file);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _fileService.Verify(
            service => service.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_WhenFileIsValid_SavesFileAndUpdatesUser()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var file = CreateFormFile("avatar.png", "image/png");
        var user = new User { UserId = 2, Email = "student@local.dev", FullName = "Demo Student" };

        _fileService
            .Setup(service => service.IsValidFile(file, It.IsAny<string[]>(), It.IsAny<long>()))
            .Returns(true);
        _fileService
            .Setup(service => service.SaveFileAsync(file, "avatars"))
            .ReturnsAsync("/uploads/avatars/avatar.png");
        _userRepository
            .Setup(repository => repository.GetUserByIdAsync(2))
            .ReturnsAsync(user);

        // Act
        var result = await controller.UploadAvatar(file);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("/uploads/avatars/avatar.png", user.AvatarUrl);
        _userRepository.Verify(repository => repository.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UploadLessonFile_WhenUserIsNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController(userId: 10, role: "teacher");
        var file = CreateFormFile("lesson.pdf", "application/pdf");
        var lesson = new Lesson { LessonId = 7, Title = "Intro" };

        _lessonRepository
            .Setup(repository => repository.GetByIdAsync(lesson.LessonId))
            .ReturnsAsync(lesson);
        _lessonRepository
            .Setup(repository => repository.IsAuthorizedAsync(lesson.LessonId, 10, "teacher"))
            .ReturnsAsync(false);

        // Act
        var result = await controller.UploadLessonFile(lesson.LessonId, file, "Lecture");

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UploadLessonFile_WhenFileIsValid_UpdatesLessonFileFields()
    {
        // Arrange
        var controller = CreateController(userId: 10, role: "teacher");
        var file = CreateFormFile("lesson.pdf", "application/pdf");
        var lesson = new Lesson { LessonId = 7, Title = "Intro" };

        _lessonRepository
            .Setup(repository => repository.GetByIdAsync(lesson.LessonId))
            .ReturnsAsync(lesson);
        _lessonRepository
            .Setup(repository => repository.IsAuthorizedAsync(lesson.LessonId, 10, "teacher"))
            .ReturnsAsync(true);
        _fileService
            .Setup(service => service.IsValidFile(file, It.IsAny<string[]>(), It.IsAny<long>()))
            .Returns(true);
        _fileService
            .Setup(service => service.SaveFileAsync(file, "lesson-files"))
            .ReturnsAsync("/uploads/lesson-files/lesson.pdf");

        // Act
        var result = await controller.UploadLessonFile(lesson.LessonId, file, "Lecture");

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("lesson.pdf", lesson.FileName);
        Assert.Equal("/uploads/lesson-files/lesson.pdf", lesson.FileUrl);
        Assert.Equal("application/pdf", lesson.FileType);
        Assert.Equal(file.Length, lesson.FileSize);
        _lessonRepository.Verify(repository => repository.UpdateAsync(lesson), Times.Once);
    }

    [Fact]
    public async Task DownloadFile_WhenFileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 2);

        // Act
        var result = await controller.DownloadFile("/uploads/missing-file.pdf");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    private FilesController CreateController(int userId, string role = "student")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileSettings:AllowedAvatarExtensions:0"] = ".jpg",
                ["FileSettings:AllowedAvatarExtensions:1"] = ".png",
                ["FileSettings:MaxAvatarSizeMB"] = "5",
                ["FileSettings:AllowedLessonExtensions:0"] = ".pdf",
                ["FileSettings:AllowedLessonExtensions:1"] = ".txt",
                ["FileSettings:MaxLessonFileSizeMB"] = "50"
            })
            .Build();

        var controller = new FilesController(
            _fileService.Object,
            _userRepository.Object,
            _lessonRepository.Object,
            configuration,
            _logger.Object);

        controller.ControllerContext = CreateControllerContext(userId, role);
        return controller;
    }

    private static ControllerContext CreateControllerContext(int userId, string role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            "TestAuth");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var bytes = "test file content"u8.ToArray();
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}


