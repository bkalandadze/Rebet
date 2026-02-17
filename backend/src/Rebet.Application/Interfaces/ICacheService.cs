namespace Rebet.Application.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache or sets it using the factory method if not found.
    /// Checks L1 (memory cache) first, then L2 (Redis), then calls factory if not found.
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="factory">The factory method to generate the value if not cached</param>
    /// <param name="expiration">The expiration time for L2 cache (Redis). L1 cache uses 30 seconds.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached or newly generated value</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a specific cache key from both L1 and L2 caches.
    /// </summary>
    /// <param name="key">The cache key to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cache keys matching the specified pattern from both L1 and L2 caches.
    /// </summary>
    /// <param name="pattern">The pattern to match (e.g., "user:*" or "position:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

