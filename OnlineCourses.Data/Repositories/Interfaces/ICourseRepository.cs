using OnlineCourses.Models.DTOs;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(int id);
    Task<IEnumerable<Course>> GetAllAsync(bool includeUnpublished = false);
    Task<IEnumerable<Course>> GetByAuthorIdAsync(int authorId);
    Task<IEnumerable<Course>> GetPublishedCoursesAsync();
    Task<(IEnumerable<Course> Items, int TotalCount)> GetFilteredAsync(CourseFilterParams filter);
    Task<Course> CreateAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
    Task<bool> ExistsAsync(int id);
    Task<int> GetStudentsCountAsync(int courseId);
    Task<IEnumerable<CourseCategoryDto>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> CreateCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(Category category);
    Task<bool> CategoryNameExistsAsync(string name, int? excludeCategoryId = null);
}
