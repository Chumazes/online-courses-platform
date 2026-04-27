using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineCourses.API.Controllers;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new RegisterDto
        {
            Email = "student@local.dev",
            Password = "123456",
            FullName = "Demo Student"
        };

        _userRepository
            .Setup(repository => repository.UserExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _userRepository.Verify(
            repository => repository.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Register_WhenEmailIsNew_CreatesStudentAndReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var request = new RegisterDto
        {
            Email = "new@student.dev",
            Password = "123456",
            FullName = "New Student"
        };

        _userRepository
            .Setup(repository => repository.UserExistsAsync(request.Email))
            .ReturnsAsync(false);
        _userRepository
            .Setup(repository => repository.CreateUserAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync((User user, string _) => user);

        // Act
        var result = await controller.Register(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _userRepository.Verify(repository => repository.CreateUserAsync(
            It.Is<User>(user =>
                user.Email == request.Email &&
                user.FullName == request.FullName &&
                user.Role == "student"),
            request.Password), Times.Once);
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var user = CreateUser();
        var request = new LoginDto { Email = user.Email, Password = "wrong-password" };

        _userRepository
            .Setup(repository => repository.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userRepository
            .Setup(repository => repository.ValidatePasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        _tokenService.Verify(service => service.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokenPair()
    {
        // Arrange
        var controller = CreateController();
        var user = CreateUser();
        var request = new LoginDto { Email = user.Email, Password = "123456" };

        _userRepository
            .Setup(repository => repository.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userRepository
            .Setup(repository => repository.ValidatePasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _tokenService
            .Setup(service => service.GenerateAccessToken(user))
            .Returns("access-token");
        _tokenService
            .Setup(service => service.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal(user.Email, response.Email);
        _userRepository.Verify(repository => repository.UpdateUserAsync(user), Times.Once);
        _tokenService.Verify(service => service.SaveRefreshTokenAsync(
            user.UserId,
            "refresh-token",
            It.Is<DateTime>(expiresAt => expiresAt > DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserExists_ReturnsOk()
    {
        // Arrange
        var user = CreateUser();
        var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(user.UserId);

        _userRepository
            .Setup(repository => repository.GetUserByIdAsync(user.UserId))
            .ReturnsAsync(user);

        // Act
        var result = await controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    private AuthController CreateController()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        return new AuthController(
            _userRepository.Object,
            _tokenService.Object,
            configuration,
            _logger.Object);
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

    private static User CreateUser() => new()
    {
        UserId = 2,
        Email = "student@local.dev",
        FullName = "Demo Student",
        Role = "student",
        IsActive = true
    };
}


