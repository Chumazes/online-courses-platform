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
        string? level = null,
        int? categoryId = null,
        string? search = null,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
            $"all={all.ToString().ToLowerInvariant()}"
        };

        if (!string.IsNullOrWhiteSpace(level))
        {
            query.Add($"level={Uri.EscapeDataString(level)}");
        }

        if (categoryId is > 0)
        {
            query.Add($"categoryId={categoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        }

        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            query.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
        }

        var url = $"api/courses?{string.Join("&", query)}";
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
