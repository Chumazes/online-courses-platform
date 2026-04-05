using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    
    public AuthController(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (await _userRepository.UserExistsAsync(request.Email))
        {
            return BadRequest(new { message = "User with this email already exists" });
        }
        
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = "student"
        };
        
        await _userRepository.CreateUserAsync(user, request.Password);
        
        return Ok(new { message = "User registered successfully" });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        
        if (user == null || !await _userRepository.ValidatePasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
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
            return Unauthorized();
        }
        
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
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
}