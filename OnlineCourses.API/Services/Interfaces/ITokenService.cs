using OnlineCourses.Models.Entities;

namespace OnlineCourses.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt);
    Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
    Task RevokeRefreshTokenAsync(int userId, string refreshToken);
}