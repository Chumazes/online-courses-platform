using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class CoursesClient : ApiClientBase
{
    public CoursesClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<PaginatedResponse<CourseResponseDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        bool all = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/courses?pageNumber={pageNumber}&pageSize={pageSize}&all={all.ToString().ToLower()}";
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            url,
            cancellationToken: cancellationToken);

        return await SendAsync<PaginatedResponse<CourseResponseDto>>(request, cancellationToken);
    }
}
