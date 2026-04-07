using System.Net.Http.Headers;
using OnlineCourses.Client.Abstractions;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Client.Api;

public sealed class FilesClient : ApiClientBase
{
    private readonly Uri? _baseAddress;

    public FilesClient(HttpClient httpClient, ITokenStore tokenStore)
        : base(httpClient, tokenStore)
    {
        _baseAddress = httpClient.BaseAddress;
    }

    public async Task<AvatarUploadDto> UploadAvatarAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));

        using var form = new MultipartFormDataContent();
        form.Add(streamContent, "file", Path.GetFileName(filePath));

        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/files/avatar",
            withBearerToken: true,
            cancellationToken: cancellationToken);

        request.Content = form;

        return await SendAsync<AvatarUploadDto>(request, cancellationToken);
    }

    public string? BuildDownloadUrl(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl) || _baseAddress is null)
        {
            return null;
        }

        var relative = $"api/files/download?fileUrl={Uri.EscapeDataString(fileUrl)}";
        return new Uri(_baseAddress, relative).ToString();
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
