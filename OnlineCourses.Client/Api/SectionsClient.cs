using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class SectionsClient : ApiClientBase
{
    public SectionsClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<IReadOnlyList<SectionResponseDto>> GetByCourseIdAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/courses/{courseId}/sections",
            cancellationToken: cancellationToken);

        return await SendAsync<List<SectionResponseDto>>(request, cancellationToken);
    }
}
