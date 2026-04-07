using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class LessonsClient : ApiClientBase
{
    public LessonsClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<IReadOnlyList<LessonResponseDto>> GetBySectionIdAsync(
        int sectionId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/sections/{sectionId}/lessons",
            cancellationToken: cancellationToken);

        return await SendAsync<List<LessonResponseDto>>(request, cancellationToken);
    }
}
