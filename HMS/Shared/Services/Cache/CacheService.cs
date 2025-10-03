using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Shared.Services.Cache;

public class CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger) : ICacheService
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromMinutes(5);

    public T? Get<T>(string key)
    {
        try
        {
            if (memoryCache.TryGetValue(key, out var value))
            {
                logger.LogDebug("Cache hit for key: {Key}", key);
                return (T?)value;
            }

            logger.LogDebug("Cache miss for key: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return default(T);
        }
    }

    public void Set<T>(string key, T value)
    {
        Set(key, value, defaultExpiration);
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = TimeSpan.FromMinutes(1), // Renova se acessado
                Priority = CacheItemPriority.Normal
            };

            memoryCache.Set(key, value, options);
            logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public void Set<T>(string key, T value, DateTimeOffset absoluteExpiration)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Priority = CacheItemPriority.Normal
            };

            memoryCache.Set(key, value, options);
            logger.LogDebug("Cache set for key: {Key}, absolute expiration: {Expiration}", key, absoluteExpiration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null)
    {
        try
        {
            if (memoryCache.TryGetValue(key, out var cachedValue))
            {
                logger.LogDebug("Cache hit for key: {Key}", key);
                return (T)cachedValue!;
            }

            logger.LogDebug("Cache miss for key: {Key}, executing function", key);
            var value = await getItem();

            Set(key, value, expiration ?? defaultExpiration);

            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    public void Remove(string key)
    {
        try
        {
            memoryCache.Remove(key);
            logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }

    public bool Exists(string key)
    {
        try
        {
            return memoryCache.TryGetValue(key, out _);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public void Clear()
    {
        try
        {
            if (memoryCache is MemoryCache concreteCache)
            {
                concreteCache.Clear();
                logger.LogInformation("Cache cleared successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache");
        }
    }
}