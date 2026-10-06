using JobApplicationTrackerAPI.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;

namespace JobApplicationTrackerAPI.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly bool _isConfigured;

    public RedisCacheService(IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            _isConfigured = false;
            _connection = null!;
            _database = null!;
            return;
        }

        _isConfigured = true;
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _database = _connection.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured) return default;

        var value = await _database.StringGetAsync(key);
        // Explicit cast: RedisValue converts to both string and ReadOnlySpan<byte>,
        // which would make the Deserialize overload ambiguous.
        var raw = (string?)value;
        if (string.IsNullOrEmpty(raw))
            return default;

        return JsonSerializer.Deserialize<T>(raw);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured) return;

        var json = JsonSerializer.Serialize(value);
        // StackExchange.Redis v3 takes an Expiration struct instead of TimeSpan?.
        // There is an implicit conversion from TimeSpan; Expiration.Default = no expiry.
        Expiration expiry = expiration.HasValue ? (Expiration)expiration.Value : Expiration.Default;
        await _database.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured) return;

        await _database.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured) return;

        // Note: Redis doesn't support prefix-based deletion natively
        // We use KEYS command which is O(N) - acceptable for cache invalidation
        var endpoints = _connection.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _connection.GetServer(endpoint);
            var keys = server.Keys(pattern: $"{prefix}*");

            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }
        }
    }
}
