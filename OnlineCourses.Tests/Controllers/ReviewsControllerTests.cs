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

public class ReviewsControllerTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<ReviewsController>> _logger = new();

    [Fact]
    public async Task GetCourseReviews_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(userId: 2);

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(404))
            .ReturnsAsync((Course?)null);

        // Act
        var result = await controller.GetCourseReviews(404);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCourseRating_WhenCourseExists_ReturnsRoundedRating()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var course = CreateCourse(courseId: 1);

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(course.CourseId))
            .ReturnsAsync(course);
        _cacheService
            .Setup(service => service.Get<CourseRatingDto>("course_rating_1"))
            .Returns((CourseRatingDto?)null);
        _reviewRepository
            .Setup(repository => repository.GetAverageRatingAsync(course.CourseId))
            .ReturnsAsync(4.666);
        _reviewRepository
            .Setup(repository => repository.GetReviewsCountAsync(course.CourseId))
            .ReturnsAsync(3);
        _reviewRepository
            .Setup(repository => repository.GetRatingDistributionAsync(course.CourseId))
            .ReturnsAsync(new Dictionary<int, int> { [5] = 2, [4] = 1 });

        // Act
        var result = await controller.GetCourseRating(course.CourseId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CourseRatingDto>(okResult.Value);
        Assert.Equal(4.67, response.AverageRating);
        Assert.Equal(3, response.TotalReviews);
        _cacheService.Verify(service => service.Set(
            "course_rating_1",
            It.Is<CourseRatingDto>(rating => rating.CourseId == course.CourseId),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task CreateReview_WhenUserIsNotEnrolled_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new CreateReviewDto { Rating = 5, Comment = "Good course" };

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(CreateCourse(courseId: 1));
        _enrollmentRepository
            .Setup(repository => repository.IsUserEnrolledAsync(2, 1))
            .ReturnsAsync(false);

        // Act
        var result = await controller.CreateReview(1, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _reviewRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<Review>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateReview_WhenUserIsEnrolled_CreatesUnapprovedReviewAndInvalidatesCaches()
    {
        // Arrange
        var controller = CreateController(userId: 2);
        var request = new CreateReviewDto { Rating = 5, Comment = "Good course" };

        _courseRepository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(CreateCourse(courseId: 1));
        _enrollmentRepository
            .Setup(repository => repository.IsUserEnrolledAsync(2, 1))
            .ReturnsAsync(true);
        _reviewRepository
            .Setup(repository => repository.GetByUserAndCourseAsync(2, 1))
            .ReturnsAsync((Review?)null);

        // Act
        var result = await controller.CreateReview(1, request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _reviewRepository.Verify(repository => repository.CreateAsync(
            It.Is<Review>(review =>
                review.UserId == 2 &&
                review.CourseId == 1 &&
                review.Rating == 5 &&
                review.Comment == request.Comment &&
                review.IsApproved == false)), Times.Once);
        _cacheService.Verify(service => service.Remove("course_reviews_1_public"), Times.Once);
        _cacheService.Verify(service => service.Remove("course_rating_1"), Times.Once);
        _cacheService.Verify(service => service.RemoveByPrefix("courses_filtered_"), Times.Once);
    }

    private ReviewsController CreateController(int userId)
    {
        var controller = new ReviewsController(
            _reviewRepository.Object,
            _courseRepository.Object,
            _enrollmentRepository.Object,
            _cacheService.Object,
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


