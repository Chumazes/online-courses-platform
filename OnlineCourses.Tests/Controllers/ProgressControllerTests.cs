using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineCourses.API.Controllers;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Tests.Controllers;

public class ProgressControllerTests
{
    private readonly Mock<IProgressRepository> _progressRepository = new();
    private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
    private readonly Mock<ILessonRepository> _lessonRepository = new();
    private readonly Mock<ILogger<ProgressController>> _logger = new();

    [Fact]
    public async Task UpdateProgress_WhenLessonDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new UpdateProgressDto { LessonId = 404, IsCompleted = true, WatchTime = 300 };

        _lessonRepository
            .Setup(repository => repository.GetByIdAsync(request.LessonId))
            .ReturnsAsync((Lesson?)null);

        // Act
        var result = await controller.UpdateProgress(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _progressRepository.Verify(
            repository => repository.CreateOrUpdateProgressAsync(It.IsAny<LessonProgress>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProgress_WhenUserIsNotEnrolled_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var lesson = CreateLesson(courseId: 1);
        var request = new UpdateProgressDto { LessonId = lesson.LessonId, IsCompleted = true, WatchTime = 300 };

        _lessonRepository
            .Setup(repository => repository.GetByIdAsync(request.LessonId))
            .ReturnsAsync(lesson);
        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, 1))
            .ReturnsAsync((Enrollment?)null);

        // Act
        var result = await controller.UpdateProgress(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProgress_WhenUserIsEnrolled_SavesProgressAndRecalculatesCourse()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var lesson = CreateLesson(courseId: 1);
        var enrollment = new Enrollment { EnrollmentId = 25, UserId = 2, CourseId = 1, Status = "active" };
        var request = new UpdateProgressDto { LessonId = lesson.LessonId, IsCompleted = true, WatchTime = 600 };

        _lessonRepository
            .Setup(repository => repository.GetByIdAsync(request.LessonId))
            .ReturnsAsync(lesson);
        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, 1))
            .ReturnsAsync(enrollment);

        // Act
        var result = await controller.UpdateProgress(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _progressRepository.Verify(repository => repository.CreateOrUpdateProgressAsync(
            It.Is<LessonProgress>(progress =>
                progress.EnrollmentId == enrollment.EnrollmentId &&
                progress.LessonId == request.LessonId &&
                progress.IsCompleted &&
                progress.WatchTime == 600)), Times.Once);
        _progressRepository.Verify(
            repository => repository.UpdateEnrollmentProgressAsync(enrollment.EnrollmentId),
            Times.Once);
    }

    [Fact]
    public async Task GetCourseProgress_WhenUserIsEnrolled_ReturnsCourseProgress()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var enrollment = new Enrollment
        {
            EnrollmentId = 25,
            UserId = 2,
            CourseId = 1,
            OverallProgress = 50,
            Status = "active",
            Course = new Course { CourseId = 1, Title = "C# Basics" }
        };

        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, 1))
            .ReturnsAsync(enrollment);
        _progressRepository
            .Setup(repository => repository.GetTotalLessonsCountAsync(1))
            .ReturnsAsync(4);
        _progressRepository
            .Setup(repository => repository.GetCompletedLessonsCountAsync(enrollment.EnrollmentId))
            .ReturnsAsync(2);

        // Act
        var result = await controller.GetCourseProgress(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CourseProgressResponseDto>(okResult.Value);
        Assert.Equal(4, response.TotalLessons);
        Assert.Equal(2, response.CompletedLessons);
        Assert.Equal(50, response.OverallProgress);
    }

    private ProgressController CreateController(int userId)
    {
        var controller = new ProgressController(
            _progressRepository.Object,
            _enrollmentRepository.Object,
            _lessonRepository.Object,
            _logger.Object);

        controller.ControllerContext = CreateControllerContext(userId);
        return controller;
    }

    private static ControllerContext CreateControllerContext(int userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
            "TestAuth");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static Lesson CreateLesson(int courseId) => new()
    {
        LessonId = 7,
        Title = "Intro lesson",
        SectionId = 3,
        Section = new Section
        {
            SectionId = 3,
            CourseId = courseId
        }
    };
}


