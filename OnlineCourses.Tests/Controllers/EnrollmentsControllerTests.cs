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

public class EnrollmentsControllerTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ILogger<EnrollmentsController>> _logger = new();

    [Fact]
    public async Task EnrollInCourse_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new EnrollmentRequestDto { CourseId = 404 };

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(request.CourseId))
            .ReturnsAsync((Course?)null);

        // Act
        var result = await controller.EnrollInCourse(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _enrollmentRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<Enrollment>()),
            Times.Never);
    }

    [Fact]
    public async Task EnrollInCourse_WhenAlreadyActiveEnrollmentExists_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new EnrollmentRequestDto { CourseId = 1 };

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(request.CourseId))
            .ReturnsAsync(CreateCourse(request.CourseId));
        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, request.CourseId))
            .ReturnsAsync(new Enrollment { EnrollmentId = 10, UserId = 2, CourseId = request.CourseId, Status = "active" });

        // Act
        var result = await controller.EnrollInCourse(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task EnrollInCourse_WhenNoEnrollmentExists_CreatesActiveEnrollment()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new EnrollmentRequestDto { CourseId = 1 };
        var course = CreateCourse(request.CourseId);

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(request.CourseId))
            .ReturnsAsync(course);
        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, request.CourseId))
            .ReturnsAsync((Enrollment?)null);
        _enrollmentRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment enrollment) =>
            {
                enrollment.EnrollmentId = 15;
                return enrollment;
            });

        // Act
        var result = await controller.EnrollInCourse(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EnrollmentResponseDto>(okResult.Value);
        Assert.Equal(15, response.EnrollmentId);
        Assert.Equal("active", response.Status);
        Assert.Equal(course.Title, response.CourseTitle);
        _enrollmentRepository.Verify(repository => repository.CreateAsync(
            It.Is<Enrollment>(enrollment =>
                enrollment.UserId == 2 &&
                enrollment.CourseId == request.CourseId &&
                enrollment.Status == "active" &&
                enrollment.OverallProgress == 0)), Times.Once);
    }

    [Fact]
    public async Task UnenrollFromCourse_WhenEnrollmentExists_MarksEnrollmentExpired()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var enrollment = new Enrollment { EnrollmentId = 15, UserId = 2, CourseId = 1, Status = "active" };

        _enrollmentRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, 1))
            .ReturnsAsync(enrollment);

        // Act
        var result = await controller.UnenrollFromCourse(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("expired", enrollment.Status);
        _enrollmentRepository.Verify(repository => repository.UpdateAsync(enrollment), Times.Once);
    }

    private EnrollmentsController CreateController(int userId)
    {
        var controller = new EnrollmentsController(
            _enrollmentRepository.Object,
            _courseRepository.Object,
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

    private static Course CreateCourse(int courseId) => new()
    {
        CourseId = courseId,
        Title = "C# Basics",
        Description = "Intro course",
        AuthorId = 10,
        Author = new User { UserId = 10, FullName = "Teacher User" }
    };
}


