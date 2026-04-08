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

    public async Task<LessonResponseDto> CreateAsync(
        int sectionId,
        CreateLessonDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/sections/{sectionId}/lessons",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<LessonResponseDto>(request, cancellationToken);
    }

    public async Task UpdateAsync(
        int sectionId,
        int lessonId,
        UpdateLessonDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            $"api/sections/{sectionId}/lessons/{lessonId}",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(
        int sectionId,
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/sections/{sectionId}/lessons/{lessonId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }
}
