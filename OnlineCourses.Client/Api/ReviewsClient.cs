using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class ReviewsClient : ApiClientBase
{
    public ReviewsClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
    }

    public async Task<IReadOnlyList<ReviewResponseDto>> GetCourseReviewsAsync(
        int courseId,
        bool all = false,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/reviews/course/{courseId}?all={all.ToString().ToLowerInvariant()}",
            cancellationToken: cancellationToken);

        return await SendAsync<List<ReviewResponseDto>>(request, cancellationToken);
    }

    public async Task<CourseRatingDto> GetCourseRatingAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/reviews/course/{courseId}/rating",
            cancellationToken: cancellationToken);

        return await SendAsync<CourseRatingDto>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewResponseDto>> GetMyAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/reviews/my",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<List<ReviewResponseDto>>(request, cancellationToken);
    }

    public async Task CreateAsync(
        int courseId,
        CreateReviewDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/reviews/course/{courseId}",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task UpdateAsync(
        int reviewId,
        UpdateReviewDto dto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            $"api/reviews/{reviewId}",
            dto,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/reviews/{reviewId}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }

    public async Task ApproveAsync(
        int reviewId,
        bool approve = true,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            $"api/reviews/{reviewId}/approve?approve={approve.ToString().ToLowerInvariant()}",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        await SendAsync(request, cancellationToken);
    }
}
