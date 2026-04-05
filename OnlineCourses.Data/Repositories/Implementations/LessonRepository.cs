using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class LessonRepository : ILessonRepository
{
    private readonly AppDbContext _context;
    
    public LessonRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Lesson?> GetByIdAsync(int id)
    {
        return await _context.Lessons
            .Include(l => l.Section)
            .ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(l => l.LessonId == id);
    }
    
    public async Task<IEnumerable<Lesson>> GetBySectionIdAsync(int sectionId)
    {
        return await _context.Lessons
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.LessonOrder)
            .ToListAsync();
    }
    
    public async Task<Lesson> CreateAsync(Lesson lesson)
    {
        lesson.CreatedAt = DateTime.UtcNow;
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }
    
    public async Task UpdateAsync(Lesson lesson)
    {
        lesson.UpdatedAt = DateTime.UtcNow;
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Lesson lesson)
    {
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Lessons.AnyAsync(l => l.LessonId == id);
    }
    
    public async Task<bool> IsAuthorizedAsync(int lessonId, int userId, string userRole)
    {
        if (userRole == "admin") return true;
        
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(l => l.LessonId == lessonId);
        
        if (lesson == null) return false;
        
        return lesson.Section?.Course.AuthorId == userId;
    }
}