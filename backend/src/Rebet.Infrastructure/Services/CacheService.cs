using System.Text;
using System.Text.Json;
using Rebet.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace Rebet.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly string _redisInstancePrefix;
    private static readonly TimeSpan L1CacheExpiration = TimeSpan.FromSeconds(30);
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        string? redisInstancePrefix = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _redisInstancePrefix = redisInstancePrefix ?? string.Empty;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IConnectionMultiplexer redisConnection,
        string? redisInstancePrefix = null)
        : this(memoryCache, distributedCache, redisInstancePrefix)
    {
        _redisConnection = redisConnection;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Check L1 cache (Memory Cache) - 30 second expiry
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            return cachedValue;
        }

        // Check L2 cache (Redis)
        var distributedValue = await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        if (distributedValue != null)
        {
            // Store in L1 cache for faster subsequent access
            _memoryCache.Set(key, distributedValue, L1CacheExpiration);
            return distributedValue;
        }

        // Not found in either cache, call factory method
        var value = await factory();

        // Store in both cache layers
        _memoryCache.Set(key, value, L1CacheExpiration);

        var expirationTime = expiration ?? TimeSpan.FromMinutes(15); // Default 15 minutes
        await SetInDistributedCacheAsync(key, value, expirationTime, cancellationToken);

        return value;
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        // Remove from L1 cache
        _memoryCache.Remove(key);

        // Remove from L2 cache
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Remove from L1 cache - we need to track keys or clear all
        // For simplicity, we'll only clear pattern-based keys from Redis
        // L1 cache will naturally expire after 30 seconds

        // Remove from L2 cache (Redis) using pattern matching
        if (_redisConnection != null)
        {
            try
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                // Prepend instance prefix to pattern for Redis key matching
                var redisPattern = string.IsNullOrEmpty(_redisInstancePrefix) 
                    ? pattern 
                    : $"{_redisInstancePrefix}{pattern}";
                
                var keys = server.Keys(pattern: redisPattern);

                var tasks = new List<Task>();
                foreach (var key in keys)
                {
                    var keyString = key.ToString();
                    // Remove instance prefix to get the original key for IDistributedCache
                    var originalKey = string.IsNullOrEmpty(_redisInstancePrefix) || !keyString.StartsWith(_redisInstancePrefix)
                        ? keyString
                        : keyString.Substring(_redisInstancePrefix.Length);
                    
                    tasks.Add(_distributedCache.RemoveAsync(originalKey, cancellationToken));
                    // Also remove from L1 cache if it exists (using original key)
                    _memoryCache.Remove(originalKey);
                }

                await Task.WhenAll(tasks);
            }
            catch
            {
                // If Redis pattern matching fails, silently continue
                // L1 cache will naturally expire after 30 seconds
            }
        }
    }

    private async Task<T?> GetFromDistributedCacheAsync<T>(string key, CancellationToken cancellationToken)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }

            var json = Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            // If Redis is unavailable, return null to fall back to factory
            return default;
        }
    }

    private async Task SetInDistributedCacheAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch
        {
            // If Redis is unavailable, silently fail - L1 cache will still work
        }
    }
}

