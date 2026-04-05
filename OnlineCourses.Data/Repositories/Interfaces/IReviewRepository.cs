using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id);
    Task<Review?> GetByUserAndCourseAsync(int userId, int courseId);
    Task<IEnumerable<Review>> GetByCourseIdAsync(int courseId, bool onlyApproved = true);
    Task<IEnumerable<Review>> GetByUserIdAsync(int userId);
    Task<Review> CreateAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Review review);
    Task<bool> ExistsAsync(int id);
    Task<bool> UserHasReviewedAsync(int userId, int courseId);
    Task<double> GetAverageRatingAsync(int courseId);
    Task<int> GetReviewsCountAsync(int courseId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(int courseId);
}