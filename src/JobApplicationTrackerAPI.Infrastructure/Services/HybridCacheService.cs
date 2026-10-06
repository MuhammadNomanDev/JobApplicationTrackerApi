using JobApplicationTrackerAPI.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace JobApplicationTrackerAPI.Infrastructure.Services;

/// <summary>
/// <see cref="ICacheService"/> backed by the framework's <see cref="HybridCache"/>:
/// an in-memory L1 cache always, with Redis as L2 when a connection string is
/// configured (see <c>DependencyInjection</c>). Replaces <c>RedisCacheService</c>
/// in P1/M1d.
///
/// Unlike its predecessor, this service caches locally even when Redis is not
/// configured — the old one silently no-op'd.
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;

    public HybridCacheService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = expiration.HasValue
            ? new HybridCacheEntryOptions { Expiration = expiration.Value }
            : null;

        return await _cache.GetOrCreateAsync(
            key,
            ct => new ValueTask<T>(factory(ct)),
            options,
            tags,
            cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync(tag, cancellationToken);
    }
}
