using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class ProgressRepository : IProgressRepository
{
    private readonly AppDbContext _context;
    
    public ProgressRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<LessonProgress?> GetProgressAsync(int enrollmentId, int lessonId)
    {
        return await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentId == enrollmentId && lp.LessonId == lessonId);
    }
    
    public async Task<IEnumerable<LessonProgress>> GetProgressByEnrollmentAsync(int enrollmentId)
    {
        return await _context.LessonProgresses
            .Include(lp => lp.Lesson)
            .Where(lp => lp.EnrollmentId == enrollmentId)
            .ToListAsync();
    }
    
    public async Task<LessonProgress> CreateOrUpdateProgressAsync(LessonProgress progress)
    {
        var existing = await GetProgressAsync(progress.EnrollmentId, progress.LessonId);
        
        if (existing != null)
        {
            existing.IsCompleted = progress.IsCompleted;
            existing.WatchTime = progress.WatchTime;
            existing.LastAccessed = DateTime.UtcNow;
            if (progress.IsCompleted && !existing.IsCompleted)
            {
                existing.CompletedAt = DateTime.UtcNow;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            
            _context.LessonProgresses.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
        
        progress.CreatedAt = DateTime.UtcNow;
        progress.LastAccessed = DateTime.UtcNow;
        _context.LessonProgresses.Add(progress);
        await _context.SaveChangesAsync();
        return progress;
    }
    
    public async Task<int> GetCompletedLessonsCountAsync(int enrollmentId)
    {
        return await _context.LessonProgresses
            .Where(lp => lp.EnrollmentId == enrollmentId && lp.IsCompleted)
            .CountAsync();
    }
    
    public async Task<int> GetTotalLessonsCountAsync(int courseId)
    {
        return await _context.Lessons
            .Include(l => l.Section)
            .Where(l => l.Section.CourseId == courseId)
            .CountAsync();
    }
    
    public async Task UpdateEnrollmentProgressAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
        
        if (enrollment == null) return;
        
        var completedLessons = await GetCompletedLessonsCountAsync(enrollmentId);
        var totalLessons = await GetTotalLessonsCountAsync(enrollment.CourseId);
        
        if (totalLessons > 0)
        {
            enrollment.OverallProgress = (int)((double)completedLessons / totalLessons * 100);
        }
        
        if (enrollment.OverallProgress >= 100 && enrollment.Status != "completed")
        {
            enrollment.Status = "completed";
            enrollment.CompletedAt = DateTime.UtcNow;
        }
        
        enrollment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}