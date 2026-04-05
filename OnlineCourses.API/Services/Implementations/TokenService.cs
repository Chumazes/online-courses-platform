using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OnlineCourses.Data;
using OnlineCourses.API.Services.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.API.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    
    public TokenService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }
    
    public string GenerateAccessToken(User user)
    {
        var secret = _configuration["JwtSettings:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    public async Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt)
    {
        var token = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);
        
        return token != null && !token.IsRevoked && token.ExpiresAt > DateTime.UtcNow;
    }
    
    public async Task RevokeRefreshTokenAsync(int userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);
        
        if (token != null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }
}