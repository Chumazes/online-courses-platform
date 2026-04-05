using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(int id);
    Task<IEnumerable<Lesson>> GetBySectionIdAsync(int sectionId);
    Task<Lesson> CreateAsync(Lesson lesson);
    Task UpdateAsync(Lesson lesson);
    Task DeleteAsync(Lesson lesson);
    Task<bool> ExistsAsync(int id);
    Task<bool> IsAuthorizedAsync(int lessonId, int userId, string userRole);
}