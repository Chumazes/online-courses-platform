using OnlineCourses.Client.Abstractions;
using OnlineCourses.Client.Models;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class AuthClient : ApiClientBase
{
    private readonly ITokenStore _tokenStore;

    public AuthClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateRequestAsync(
            HttpMethod.Post,
            "api/auth/register",
            request,
            cancellationToken: cancellationToken);

        await SendAsync(httpRequest, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateRequestAsync(
            HttpMethod.Post,
            "api/auth/login",
            request,
            cancellationToken: cancellationToken);

        var authResponse = await SendAsync<AuthResponseDto>(httpRequest, cancellationToken);

        await _tokenStore.SaveAsync(
            new StoredSession
            {
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                ExpiresAt = authResponse.ExpiresAt
            },
            cancellationToken);

        return authResponse;
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateRequestAsync(
            HttpMethod.Get,
            "api/auth/me",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<CurrentUserDto>(httpRequest, cancellationToken);
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(
        UpdateProfileDto request,
        CancellationToken cancellationToken = default)
    {
        using var httpRequest = await CreateRequestAsync(
            HttpMethod.Put,
            "api/auth/me",
            request,
            withBearerToken: true,
            cancellationToken: cancellationToken);

        return await SendAsync<CurrentUserDto>(httpRequest, cancellationToken);
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        return await TryRefreshSessionAsync(cancellationToken);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return _tokenStore.ClearAsync(cancellationToken);
    }
}
