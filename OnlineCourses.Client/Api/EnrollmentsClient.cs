using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class EnrollmentsClient : ApiClientBase
{
    public EnrollmentsClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetMyAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/enrollments/my",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<List<EnrollmentResponseDto>>(request, cancellationToken);
    }

    public async Task<EnrollmentResponseDto> EnrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/enrollments",
            new EnrollmentRequestDto { CourseId = courseId },
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<EnrollmentResponseDto>(request, cancellationToken);
    }

    public async Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/enrollments/{courseId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseEnrollmentDto>> GetCourseEnrollmentsAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/enrollments/course/{courseId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<List<CourseEnrollmentDto>>(request, cancellationToken);
    }
}
