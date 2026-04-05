using OnlineCourses.Models.Entities;

namespace OnlineCourses.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}