using Microsoft.Extensions.Caching.Memory;
using OnlineCourses.API.Services.Interfaces;
using System.Collections.Generic;
using System.Reflection;

namespace OnlineCourses.API.Services.Implementations;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
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
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        }
        
        // Добавляем приоритет
        options.Priority = CacheItemPriority.Normal;
        
        // Добавляем пост-эвакуационное действие
        options.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            _logger.LogDebug("Cache entry {Key} evicted. Reason: {Reason}", key, reason);
        });
        
        _cache.Set(key, value, options);
        _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
    }
    
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache removed for key: {Key}", key);
    }
    
    public void RemoveByPrefix(string prefix)
    {
        // В IMemoryCache нет прямого способа удалить по префиксу
        // Нужно использовать рефлексию или хранить список ключей
        // Для простоты используем рефлексию
        try
        {
            var cacheEntries = typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cacheEntries != null && cacheEntries.GetValue(_cache) is IDictionary<object, object> entries)
            {
                var keysToRemove = entries.Keys
                    .OfType<string>()
                    .Where(k => k.StartsWith(prefix))
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _logger.LogDebug("Cache removed by prefix for key: {Key}", key);
                }
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