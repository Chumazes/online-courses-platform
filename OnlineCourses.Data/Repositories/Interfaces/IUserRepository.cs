using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user, string password);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task UpdateUserAsync(User user);
    Task<bool> UserExistsAsync(string email);
}