using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Implementations;

public class SectionRepository : ISectionRepository
{
    private readonly AppDbContext _context;
    
    public SectionRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Section?> GetByIdAsync(int id)
    {
        return await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.SectionId == id);
    }
    
    public async Task<IEnumerable<Section>> GetByCourseIdAsync(int courseId)
    {
        return await _context.Sections
            .Include(s => s.Lessons)
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();
    }
    
    public async Task<Section> CreateAsync(Section section)
    {
        section.CreatedAt = DateTime.UtcNow;
        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        return section;
    }
    
    public async Task UpdateAsync(Section section)
    {
        section.UpdatedAt = DateTime.UtcNow;
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Section section)
    {
        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Sections.AnyAsync(s => s.SectionId == id);
    }
    
    public async Task<int> GetLessonsCountAsync(int sectionId)
    {
        return await _context.Lessons
            .CountAsync(l => l.SectionId == sectionId);
    }
    
    public async Task<bool> IsAuthorizedAsync(int sectionId, int userId, string userRole)
    {
        if (userRole == "admin") return true;
        
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.SectionId == sectionId);
        
        if (section == null) return false;
        
        return section.Course.AuthorId == userId;
    }
}