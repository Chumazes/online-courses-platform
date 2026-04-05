using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class CoursesClient : ApiClientBase
{
    public CoursesClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<IReadOnlyList<CourseResponseDto>> GetAllAsync(
        bool all = false,
        CancellationToken cancellationToken = default)
    {
        var url = all ? "api/courses?all=true" : "api/courses";
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);

        return await SendAsync<List<CourseResponseDto>>(request, cancellationToken);
    }
}
