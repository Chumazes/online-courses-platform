using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == normalizedEmail);
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
    }
    
    public async Task<User> CreateUserAsync(User user, string password)
    {
        user.Email = NormalizeEmail(user.Email);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.RegistrationDate = DateTime.UtcNow;
        user.CreatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
    
    public async Task<bool> ValidatePasswordAsync(User user, string password)
    {
        return await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }
    
    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> UserExistsAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == normalizedEmail);
    }
}
