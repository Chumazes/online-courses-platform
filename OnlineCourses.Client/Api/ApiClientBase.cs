using System.Net.Http.Headers;
using System.Net.Http.Json;
using OnlineCourses.Client.Abstractions;

namespace OnlineCourses.Client.Api;

public abstract class ApiClientBase
{
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
            var session = await _tokenStore.GetAsync(cancellationToken);
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                throw new ApiException("No access token found. Please login first.", System.Net.HttpStatusCode.Unauthorized);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
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
