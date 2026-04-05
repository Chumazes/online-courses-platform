using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _context;
    
    public EnrollmentRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Enrollment?> GetByIdAsync(int id)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.EnrollmentId == id);
    }
    
    public async Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
    }
    
    public async Task<IEnumerable<Enrollment>> GetByUserIdAsync(int userId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Enrollment>> GetByCourseIdAsync(int courseId)
    {
        return await _context.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId)
            .ToListAsync();
    }
    
    public async Task<Enrollment> CreateAsync(Enrollment enrollment)
    {
        enrollment.EnrollmentDate = DateTime.UtcNow;
        enrollment.Status = "active";
        enrollment.OverallProgress = 0;
        
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();
        
        return enrollment;
    }
    
    public async Task UpdateAsync(Enrollment enrollment)
    {
        enrollment.UpdatedAt = DateTime.UtcNow;
        _context.Enrollments.Update(enrollment);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> IsUserEnrolledAsync(int userId, int courseId)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
    }
    
    public async Task<int> GetEnrollmentCountAsync(int courseId)
    {
        return await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId);
    }
}