using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OnlineCourses.Client.Abstractions;
using OnlineCourses.Client.Models;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public abstract class ApiClientBase
{
    private static readonly SemaphoreSlim RefreshSync = new(1, 1);
    private static readonly TimeSpan RefreshAhead = TimeSpan.FromMinutes(1);
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;

    protected ApiClientBase(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }

    protected async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string relativeUrl,
        object? body = null,
        bool withBearerToken = false,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, relativeUrl);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (withBearerToken)
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }

    protected async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        if (result is null)
        {
            throw new ApiException("Server returned an empty response.", response.StatusCode);
        }

        return result;
    }

    protected async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected Task<bool> TryRefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        return RefreshSessionAsync(forceRefresh: true, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var session = await _tokenStore.GetAsync(cancellationToken);
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            throw new ApiException("No access token found. Please login first.", HttpStatusCode.Unauthorized);
        }

        if (!ShouldRefresh(session.ExpiresAt))
        {
            return session.AccessToken;
        }

        var refreshed = await RefreshSessionAsync(forceRefresh: false, cancellationToken);
        if (!refreshed)
        {
            if (session.ExpiresAt > DateTime.UtcNow)
            {
                return session.AccessToken;
            }

            throw new ApiException("Access token expired. Please login again.", HttpStatusCode.Unauthorized);
        }

        session = await _tokenStore.GetAsync(cancellationToken);
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            throw new ApiException("No access token found. Please login first.", HttpStatusCode.Unauthorized);
        }

        return session.AccessToken;
    }

    private async Task<bool> RefreshSessionAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        await RefreshSync.WaitAsync(cancellationToken);
        try
        {
            var session = await _tokenStore.GetAsync(cancellationToken);
            if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
            {
                return false;
            }

            if (!forceRefresh && !ShouldRefresh(session.ExpiresAt))
            {
                return true;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
            {
                Content = JsonContent.Create(new RefreshTokenDto
                {
                    RefreshToken = session.RefreshToken
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
            if (authResponse is null)
            {
                return false;
            }

            await _tokenStore.SaveAsync(
                new StoredSession
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = authResponse.ExpiresAt
                },
                cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            RefreshSync.Release();
        }
    }

    private static bool ShouldRefresh(DateTime expiresAt)
    {
        return expiresAt <= DateTime.UtcNow.Add(RefreshAhead);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        var message = $"API request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
        throw new ApiException(message, response.StatusCode, responseBody);
    }
}
