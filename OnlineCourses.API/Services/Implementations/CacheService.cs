using Microsoft.Extensions.Caching.Memory;
using OnlineCourses.API.Services.Interfaces;
using System.Collections.Concurrent;

namespace OnlineCourses.API.Services.Implementations;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
            Priority = CacheItemPriority.Normal
        };

        options.RegisterPostEvictionCallback((evictedKey, _, reason, _) =>
        {
            _logger.LogDebug("Cache entry {Key} evicted. Reason: {Reason}", evictedKey, reason);

            if (evictedKey is string cacheKey && !_cache.TryGetValue(cacheKey, out _))
            {
                _keys.TryRemove(cacheKey, out _);
            }
        });

        _keys[key] = 0;
        _cache.Set(key, value, options);
        _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("Cache removed for key: {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        try
        {
            var keysToRemove = _keys.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
                _logger.LogDebug("Cache removed by prefix for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by prefix: {Prefix}", prefix);
        }
    }

    public bool Exists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }
}
