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

    public async Task<SectionResponseDto> GetByIdAsync(
        int courseId,
        int sectionId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/courses/{courseId}/sections/{sectionId}",
            cancellationToken: cancellationToken);

        return await SendAsync<SectionResponseDto>(request, cancellationToken);
    }

    public async Task<SectionResponseDto> CreateAsync(
        int courseId,
        CreateSectionDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/courses/{courseId}/sections",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<SectionResponseDto>(request, cancellationToken);
    }

    public async Task UpdateAsync(
        int courseId,
        int sectionId,
        UpdateSectionDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            $"api/courses/{courseId}/sections/{sectionId}",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(
        int courseId,
        int sectionId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/courses/{courseId}/sections/{sectionId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }
}
