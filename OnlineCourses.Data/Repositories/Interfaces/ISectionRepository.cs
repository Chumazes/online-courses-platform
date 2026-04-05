using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface ISectionRepository
{
    Task<Section?> GetByIdAsync(int id);
    Task<IEnumerable<Section>> GetByCourseIdAsync(int courseId);
    Task<Section> CreateAsync(Section section);
    Task UpdateAsync(Section section);
    Task DeleteAsync(Section section);
    Task<bool> ExistsAsync(int id);
    Task<int> GetLessonsCountAsync(int sectionId);
    Task<bool> IsAuthorizedAsync(int sectionId, int userId, string userRole);
}