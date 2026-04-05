using System.Text.Json;
using OnlineCourses.Client.Abstractions;
using OnlineCourses.Client.Models;

namespace OnlineCourses.Client.Infrastructure;

public sealed class FileTokenStore : ITokenStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly string _filePath;

    public FileTokenStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<StoredSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<StoredSession>(stream, SerializerOptions, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task SaveAsync(StoredSession session, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, session, SerializerOptions, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}
