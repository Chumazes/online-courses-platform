using Microsoft.EntityFrameworkCore;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.Models.DTOs;
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
        
        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }
    
    public async Task<IEnumerable<Course>> GetByAuthorIdAsync(int authorId)
    {
        return await _context.Courses
            .Include(c => c.Category)
            .Where(c => c.AuthorId == authorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Course>> GetPublishedCoursesAsync()
    {
        return await _context.Courses
            .Include(c => c.Author)
            .Include(c => c.Category)
            .Where(c => c.Status == "published")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<(IEnumerable<Course> Items, int TotalCount)> GetFilteredAsync(CourseFilterParams filter)
    {
        var query = _context.Courses
            .Include(c => c.Author)
            .Include(c => c.Category)
            .AsQueryable();

        // Фильтрация по статусу (только опубликованные для всех, если не запрошены все)
        if (!filter.All)
        {
            query = query.Where(c => c.Status == "published");
        }

        // Фильтр по уровню
        if (!string.IsNullOrEmpty(filter.Level))
        {
            query = query.Where(c => c.Level == filter.Level);
        }

        // Фильтр по категории
        if (filter.CategoryId.HasValue && filter.CategoryId > 0)
        {
            query = query.Where(c => c.CategoryId == filter.CategoryId);
        }

        // Поиск по названию или описанию
        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(c => 
                c.Title.ToLower().Contains(filter.Search.ToLower()) || 
                c.Description.ToLower().Contains(filter.Search.ToLower()));
        }

        // Фильтр по цене
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(c => c.Price >= filter.MinPrice);
        }
        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= filter.MaxPrice);
        }

        // Сортировка
        query = filter.SortBy?.ToLower() switch
        {
            "title" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "price" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price),
            "rating" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.AvgRating) : query.OrderBy(c => c.AvgRating),
            "createdat" => filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
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
            .CountAsync(e => e.CourseId == courseId && e.Status == "active");
    }
    
    public async Task<IEnumerable<CourseCategoryDto>> GetCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CourseCategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId
            })
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Category category)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CategoryNameExistsAsync(string name, int? excludeCategoryId = null)
    {
        var normalized = name.Trim().ToLower();

        return await _context.Categories.AnyAsync(c =>
            c.Name.ToLower() == normalized &&
            (!excludeCategoryId.HasValue || c.CategoryId != excludeCategoryId.Value));
    }
}
