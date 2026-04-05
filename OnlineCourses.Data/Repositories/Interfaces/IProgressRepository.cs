using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface IProgressRepository
{
    Task<LessonProgress?> GetProgressAsync(int enrollmentId, int lessonId);
    Task<IEnumerable<LessonProgress>> GetProgressByEnrollmentAsync(int enrollmentId);
    Task<LessonProgress> CreateOrUpdateProgressAsync(LessonProgress progress);
    Task<int> GetCompletedLessonsCountAsync(int enrollmentId);
    Task<int> GetTotalLessonsCountAsync(int courseId);
    Task UpdateEnrollmentProgressAsync(int enrollmentId);
}