using JobApplicationTrackerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace JobApplicationTrackerAPI.IntegrationTests.Fixtures;

/// <summary>
/// Starts a throwaway SQL Server container (P1/M1e) and applies the EF Core
/// migrations to it. Shared across all database-backed tests in the
/// "SqlServerDatabase" xUnit collection — the container starts once per test
/// run, not per test class.
///
/// Requires a Docker daemon. In CI (GitHub Actions ubuntu-latest) Docker is
/// available; without one these tests fail fast at fixture startup instead of
/// silently skipping.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public SqlServerFixture()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    /// <summary>
    /// Connection string for the containerised SQL Server, with migrations applied.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
