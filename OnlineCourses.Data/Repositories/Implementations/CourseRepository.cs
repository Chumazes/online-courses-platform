using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _context;
    
    public CourseRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.Author)
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.CourseId == id);
    }
    
    public async Task<IEnumerable<Course>> GetAllAsync(bool includeUnpublished = false)
    {
        var query = _context.Courses
            .Include(c => c.Author)
            .Include(c => c.Category)
            .AsQueryable();
        
        if (!includeUnpublished)
        {
            query = query.Where(c => c.Status == "published");
        }
        
        return await query.ToListAsync();
    }
    
    public async Task<IEnumerable<Course>> GetByAuthorIdAsync(int authorId)
    {
        return await _context.Courses
            .Include(c => c.Category)
            .Where(c => c.AuthorId == authorId)
            .ToListAsync();
    }
    
    public async Task<Course> CreateAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }
    
    public async Task UpdateAsync(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        _context.Courses.Update(course);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Course course)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Courses.AnyAsync(c => c.CourseId == id);
    }
    
    public async Task<int> GetStudentsCountAsync(int courseId)
    {
        return await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId);
    }
}