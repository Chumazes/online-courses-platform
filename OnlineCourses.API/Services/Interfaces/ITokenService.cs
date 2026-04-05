using OnlineCourses.Models.Entities;

namespace OnlineCourses.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt);
    Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(int userId);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, int userId);
}