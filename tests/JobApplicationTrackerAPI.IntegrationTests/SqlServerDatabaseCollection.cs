using JobApplicationTrackerAPI.IntegrationTests.Fixtures;

namespace JobApplicationTrackerAPI.IntegrationTests;

/// <summary>
/// xUnit collection sharing one <see cref="SqlServerFixture"/> (one SQL Server
/// container) across all database-backed test classes in the run.
/// </summary>
[CollectionDefinition("SqlServerDatabase")]
public class SqlServerDatabaseCollection : ICollectionFixture<SqlServerFixture>
{
}
