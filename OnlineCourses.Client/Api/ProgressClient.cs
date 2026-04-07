using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class ProgressClient : ApiClientBase
{
    public ProgressClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<CourseProgressResponseDto> GetCourseProgressAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/progress/course/{courseId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<CourseProgressResponseDto>(request, cancellationToken);
    }

    public async Task<LessonProgressResponseDto> GetLessonProgressAsync(
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/progress/lesson/{lessonId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<LessonProgressResponseDto>(request, cancellationToken);
    }

    public async Task UpdateProgressAsync(
        UpdateProgressDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/progress/update",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }
}
