using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data.Repositories.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(int id);
    Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId);
    Task<IEnumerable<Enrollment>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Enrollment>> GetByCourseIdAsync(int courseId);
    Task<Enrollment> CreateAsync(Enrollment enrollment);
    Task UpdateAsync(Enrollment enrollment);
    Task<bool> IsUserEnrolledAsync(int userId, int courseId);
    Task<int> GetEnrollmentCountAsync(int courseId);
}