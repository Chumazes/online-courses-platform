using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;
    
    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.ReviewId == id);
    }
    
    public async Task<Review?> GetByUserAndCourseAsync(int userId, int courseId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);
    }
    
    public async Task<IEnumerable<Review>> GetByCourseIdAsync(int courseId, bool onlyApproved = true)
    {
        var query = _context.Reviews
            .Include(r => r.User)
            .Where(r => r.CourseId == courseId);
        
        if (onlyApproved)
        {
            query = query.Where(r => r.IsApproved);
        }
        
        return await query
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Review>> GetByUserIdAsync(int userId)
    {
        return await _context.Reviews
            .Include(r => r.Course)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();
    }
    
    public async Task<Review> CreateAsync(Review review)
    {
        review.ReviewDate = DateTime.UtcNow;
        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        
        // Обновляем средний рейтинг курса
        await UpdateCourseAverageRating(review.CourseId);
        
        return review;
    }
    
    public async Task UpdateAsync(Review review)
    {
        review.UpdatedAt = DateTime.UtcNow;
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
        
        // Обновляем средний рейтинг курса
        await UpdateCourseAverageRating(review.CourseId);
    }
    
    public async Task DeleteAsync(Review review)
    {
        var courseId = review.CourseId;
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        
        // Обновляем средний рейтинг курса
        await UpdateCourseAverageRating(courseId);
    }
    
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Reviews.AnyAsync(r => r.ReviewId == id);
    }
    
    public async Task<bool> UserHasReviewedAsync(int userId, int courseId)
    {
        return await _context.Reviews.AnyAsync(r => r.UserId == userId && r.CourseId == courseId);
    }
    
    public async Task<double> GetAverageRatingAsync(int courseId)
    {
        return await _context.Reviews
            .Where(r => r.CourseId == courseId && r.IsApproved)
            .AverageAsync(r => (double?)r.Rating) ?? 0;
    }
    
    public async Task<int> GetReviewsCountAsync(int courseId)
    {
        return await _context.Reviews
            .CountAsync(r => r.CourseId == courseId && r.IsApproved);
    }
    
    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(int courseId)
    {
        var distribution = await _context.Reviews
            .Where(r => r.CourseId == courseId && r.IsApproved)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Rating, g => g.Count);
        
        // Заполняем отсутствующие рейтинги нулями
        for (int i = 1; i <= 5; i++)
        {
            if (!distribution.ContainsKey(i))
            {
                distribution[i] = 0;
            }
        }
        
        return distribution;
    }
    
    private async Task UpdateCourseAverageRating(int courseId)
    {
        var avgRating = await GetAverageRatingAsync(courseId);
        var course = await _context.Courses.FindAsync(courseId);
        
        if (course != null)
        {
            course.AvgRating = (decimal)Math.Round(avgRating, 2);
            await _context.SaveChangesAsync();
        }
    }
}