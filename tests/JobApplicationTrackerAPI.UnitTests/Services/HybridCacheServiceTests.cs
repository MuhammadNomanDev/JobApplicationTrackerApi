using System.Threading;
using AwesomeAssertions;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1d: <see cref="HybridCacheService"/> behaviour against a real
/// <c>HybridCache</c> with no Redis configured (pure in-memory L1).
/// </summary>
public class HybridCacheServiceTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly ICacheService _sut;

    public HybridCacheServiceTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddSingleton<ICacheService, HybridCacheService>();
        _provider = services.BuildServiceProvider();
        _sut = _provider.GetRequiredService<ICacheService>();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task GetOrCreate_CachesValue_FactoryRunsOnce()
    {
        // Arrange
        var factoryCalls = 0;

        // Act
        var first = await _sut.GetOrCreateAsync(
            "key-1",
            ct =>
            {
                factoryCalls++;
                return Task.FromResult("value");
            },
            TimeSpan.FromMinutes(5));
        var second = await _sut.GetOrCreateAsync(
            "key-1",
            ct =>
            {
                factoryCalls++;
                return Task.FromResult("other");
            },
            TimeSpan.FromMinutes(5));

        // Assert
        first.Should().Be("value");
        second.Should().Be("value");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreate_ConcurrentCalls_FactoryRunsOnce()
    {
        // Arrange
        var factoryCalls = 0;

        // Act: 20 concurrent requests for an uncached key (thundering herd).
        await Parallel.ForEachAsync(
            Enumerable.Range(0, 20),
            CancellationToken.None,
            async (_, _) => await _sut.GetOrCreateAsync(
                "hot-key",
                async c =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    await Task.Delay(50, c);
                    return "computed";
                },
                TimeSpan.FromMinutes(5),
                cancellationToken: CancellationToken.None));

        // Assert: stampede protection — one factory execution serves all.
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task RemoveAsync_EvictsEntry()
    {
        // Arrange
        var factoryCalls = 0;
        Task<string> Factory(CancellationToken _) =>
            Task.FromResult($"v{Interlocked.Increment(ref factoryCalls)}");

        // Act
        var first = await _sut.GetOrCreateAsync("evict-me", Factory, cancellationToken: CancellationToken.None);
        await _sut.RemoveAsync("evict-me", CancellationToken.None);
        var second = await _sut.GetOrCreateAsync("evict-me", Factory, cancellationToken: CancellationToken.None);

        // Assert
        first.Should().Be("v1");
        second.Should().Be("v2");
    }

    [Fact]
    public async Task RemoveByTagAsync_EvictsOnlyTaggedEntries()
    {
        // Arrange
        var callsA = 0;
        var callsB = 0;

        await _sut.GetOrCreateAsync("a-1", _ => Task.FromResult($"a{++callsA}"), tags: ["group-a"], cancellationToken: CancellationToken.None);
        await _sut.GetOrCreateAsync("a-2", _ => Task.FromResult($"a{++callsA}"), tags: ["group-a"], cancellationToken: CancellationToken.None);
        await _sut.GetOrCreateAsync("b-1", _ => Task.FromResult($"b{++callsB}"), tags: ["group-b"], cancellationToken: CancellationToken.None);

        // Act
        await _sut.RemoveByTagAsync("group-a", CancellationToken.None);

        // Assert: tagged entries refetch, untagged entry stays cached.
        var a1 = await _sut.GetOrCreateAsync("a-1", _ => Task.FromResult($"a{++callsA}"), tags: ["group-a"], cancellationToken: CancellationToken.None);
        var b1 = await _sut.GetOrCreateAsync("b-1", _ => Task.FromResult($"b{++callsB}"), tags: ["group-b"], cancellationToken: CancellationToken.None);

        a1.Should().Be("a3");
        b1.Should().Be("b1");
        callsA.Should().Be(3);
        callsB.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreate_WithoutRedis_WorksOnLocalCacheOnly()
    {
        // Arrange: no Redis connection string is configured in this test host,
        // so HybridCache runs on its in-memory L1 only (no Act needed beyond
        // the call itself).

        // Act
        var value = await _sut.GetOrCreateAsync(
            "local-only",
            _ => Task.FromResult(42),
            TimeSpan.FromMinutes(1),
            cancellationToken: CancellationToken.None);

        // Assert
        value.Should().Be(42);
    }
}
