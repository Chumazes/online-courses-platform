using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;
using System.Security.Claims;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        IUserRepository userRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);
        
        if (await _userRepository.UserExistsAsync(request.Email))
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
            return BadRequest(new { message = "User with this email already exists" });
        }
        
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = "student"
        };
        
        await _userRepository.CreateUserAsync(user, request.Password);
        
        _logger.LogInformation("User registered successfully: {Email}, ID: {UserId}", user.Email, user.UserId);
        
        return Ok(new { message = "User registered successfully" });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);
        
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        
        if (user == null || !await _userRepository.ValidatePasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Login failed - invalid credentials for email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - account deactivated for email: {Email}", request.Email);
            return Unauthorized(new { message = "Account is deactivated" });
        }
        
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user);
        
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        var refreshTokenExpirationDays = Convert.ToDouble(
            _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"
        );
        
        await _tokenService.SaveRefreshTokenAsync(
            user.UserId,
            refreshToken,
            DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
        );
        
        _logger.LogInformation("User logged in successfully: {Email}, ID: {UserId}", user.Email, user.UserId);
        
        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
    
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetCurrentUser failed - invalid user ID claim");
            return Unauthorized();
        }
        
        _logger.LogInformation("GetCurrentUser called for user ID: {UserId}", userId);
        
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("GetCurrentUser failed - user not found: {UserId}", userId);
            return NotFound();
        }
        
        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FullName,
            user.Role,
            user.Bio,
            user.AvatarUrl
        });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateProfileDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("UpdateCurrentUser failed - invalid user ID claim");
            return Unauthorized();
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("UpdateCurrentUser failed - user not found: {UserId}", userId);
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();

        await _userRepository.UpdateUserAsync(user);

        _logger.LogInformation("User profile updated successfully: {UserId}", userId);

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FullName,
            user.Role,
            user.Bio,
            user.AvatarUrl
        });
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("Refresh token attempt");
        
        var refreshToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
        
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token failed - invalid or expired token");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        
        var user = refreshToken.User;
        
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
        
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        
        var refreshTokenExpirationDays = Convert.ToDouble(
            _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"
        );
        
        await _tokenService.SaveRefreshTokenAsync(
            user.UserId,
            newRefreshToken,
            DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
        );
        
        _logger.LogInformation("Refresh token successful for user: {Email}", user.Email);
        
        return Ok(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdClaim != null ? int.Parse(userIdClaim) : 0;
        
        _logger.LogInformation("Logout attempt for user ID: {UserId}", userId);
        
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
        
        _logger.LogInformation("User logged out successfully: {UserId}", userId);
        
        return Ok(new { message = "Logged out successfully" });
    }
    
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAllDevices()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("LogoutAllDevices failed - invalid user ID claim");
            return Unauthorized();
        }
        
        _logger.LogInformation("LogoutAllDevices attempt for user ID: {UserId}", userId);
        
        await _tokenService.RevokeAllUserTokensAsync(userId);
        
        _logger.LogInformation("User logged out from all devices: {UserId}", userId);
        
        return Ok(new { message = "Logged out from all devices" });
    }
}
