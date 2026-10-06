namespace JobApplicationTrackerAPI.Application.Interfaces.Services;

/// <summary>
/// Cache abstraction for the application layer. Implemented by
/// <c>HybridCacheService</c> (P1/M1d); previously <c>RedisCacheService</c>.
///
/// The prefix-based invalidation of the old contract
/// (<c>RemoveByPrefixAsync</c>, implemented with Redis KEYS — an O(N) scan)
/// was replaced with tag invalidation, which HybridCache supports natively.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets the cached value for <paramref name="key"/>, or creates it with
    /// <paramref name="factory"/> and caches the result. Under concurrency the
    /// factory runs at most once per key (stampede protection).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
