using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineCourses.API.Controllers;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Tests.Controllers;

public class CoursesControllerTests
{
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<CoursesController>> _logger = new();

    [Fact]
    public async Task GetCourseById_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        _cacheService
            .Setup(service => service.Get<CourseResponseDto>("course_404"))
            .Returns((CourseResponseDto?)null);
        _courseRepository
            .Setup(repository => repository.GetByIdAsync(404))
            .ReturnsAsync((Course?)null);

        // Act
        var result = await controller.GetCourseById(404);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCourseById_WhenCourseExists_ReturnsCourseDtoAndCachesIt()
    {
        // Arrange
        var controller = CreateController();
        var course = CreateCourse();

        _cacheService
            .Setup(service => service.Get<CourseResponseDto>("course_1"))
            .Returns((CourseResponseDto?)null);
        _courseRepository
            .Setup(repository => repository.GetByIdAsync(course.CourseId))
            .ReturnsAsync(course);
        _courseRepository
            .Setup(repository => repository.GetStudentsCountAsync(course.CourseId))
            .ReturnsAsync(12);

        // Act
        var result = await controller.GetCourseById(course.CourseId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CourseResponseDto>(okResult.Value);
        Assert.Equal(course.CourseId, response.CourseId);
        Assert.Equal("Programming", response.CategoryName);
        Assert.Equal("Teacher User", response.AuthorName);
        Assert.Equal(12, response.TotalStudents);
        _cacheService.Verify(service => service.Set(
            "course_1",
            It.Is<CourseResponseDto>(dto => dto.CourseId == course.CourseId),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCourses_WhenCacheMiss_ReturnsPaginatedResponseAndCachesIt()
    {
        // Arrange
        var controller = CreateController();
        var filter = new CourseFilterParams { PageNumber = 1, PageSize = 10 };
        var course = CreateCourse();

        _cacheService
            .Setup(service => service.Get<PaginatedResponse<CourseResponseDto>>(It.IsAny<string>()))
            .Returns((PaginatedResponse<CourseResponseDto>?)null);
        _courseRepository
            .Setup(repository => repository.GetFilteredAsync(filter))
            .ReturnsAsync((new[] { course }, 1));
        _courseRepository
            .Setup(repository => repository.GetStudentsCountAsync(course.CourseId))
            .ReturnsAsync(5);

        // Act
        var result = await controller.GetAllCourses(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<CourseResponseDto>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
        _cacheService.Verify(service => service.Set(
            It.Is<string>(key => key.StartsWith("courses_filtered_")),
            response,
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task CreateCourse_WhenUserClaimIsMissing_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.CreateCourse(new CreateCourseDto
        {
            Title = "C# Basics",
            Description = "Intro course",
            Level = "beginner",
            Price = 0
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _courseRepository.Verify(repository => repository.CreateAsync(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task CreateCourse_WhenTeacherIsAuthorized_CreatesDraftAndInvalidatesCaches()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(userId: 10, role: "teacher");
        var request = new CreateCourseDto
        {
            Title = "C# Basics",
            Description = "Intro course",
            Level = "beginner",
            Price = 1000,
            CategoryId = 3
        };

        _courseRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Course>()))
            .ReturnsAsync((Course course) =>
            {
                course.CourseId = 99;
                return course;
            });

        // Act
        var result = await controller.CreateCourse(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdCourse = Assert.IsType<Course>(createdResult.Value);
        Assert.Equal(nameof(CoursesController.GetCourseById), createdResult.ActionName);
        Assert.Equal(10, createdCourse.AuthorId);
        Assert.Equal("draft", createdCourse.Status);
        _cacheService.Verify(service => service.RemoveByPrefix("courses_filtered_"), Times.Once);
        _cacheService.Verify(service => service.RemoveByPrefix("courses_all_"), Times.Once);
        _cacheService.Verify(service => service.RemoveByPrefix("my_courses_"), Times.Once);
    }

    private CoursesController CreateController() => new(
        _courseRepository.Object,
        _cacheService.Object,
        _logger.Object);

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

    private static Course CreateCourse() => new()
    {
        CourseId = 1,
        Title = "C# Basics",
        Description = "Intro course",
        Price = 1000,
        Level = "beginner",
        Status = "published",
        CategoryId = 3,
        Category = new Category
        {
            CategoryId = 3,
            Name = "Programming"
        },
        AuthorId = 10,
        Author = new User
        {
            UserId = 10,
            FullName = "Teacher User",
            Email = "teacher@local.dev",
            Role = "teacher"
        },
        CreatedAt = DateTime.UtcNow
    };
}


