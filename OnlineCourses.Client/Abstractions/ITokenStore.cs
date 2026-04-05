using OnlineCourses.Client.Models;

namespace OnlineCourses.Client.Abstractions;

public interface ITokenStore
{
    Task<StoredSession?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(StoredSession session, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
