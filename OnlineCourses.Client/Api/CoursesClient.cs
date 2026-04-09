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

    public async Task<CourseResponseDto> GetByIdAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/courses/{courseId}",
            cancellationToken: cancellationToken);

        return await SendAsync<CourseResponseDto>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseResponseDto>> GetMyAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/courses/my",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<List<CourseResponseDto>>(request, cancellationToken);
    }

    public async Task<CourseResponseDto> CreateAsync(
        CreateCourseDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/courses",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<CourseResponseDto>(request, cancellationToken);
    }

    public async Task UpdateAsync(
        int courseId,
        UpdateCourseDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            $"api/courses/{courseId}",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/courses/{courseId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/courses/categories",
            cancellationToken: cancellationToken);

        return await SendAsync<List<CourseCategoryDto>>(request, cancellationToken);
    }
}
