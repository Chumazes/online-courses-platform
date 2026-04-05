using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(int id);
    Task<IEnumerable<Course>> GetAllAsync(bool includeUnpublished = false);
    Task<IEnumerable<Course>> GetByAuthorIdAsync(int authorId);
    Task<Course> CreateAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
    Task<bool> ExistsAsync(int id);
    Task<int> GetStudentsCountAsync(int courseId);
}