namespace RestauranteAPI.Tests.Database;

public sealed class SqlServerIntegrationTheoryAttribute : TheoryAttribute
{
    public SqlServerIntegrationTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                SqlServerDatabaseFixture.ConnectionStringEnvironmentVariable)))
        {
            Skip = $"Set {SqlServerDatabaseFixture.ConnectionStringEnvironmentVariable} to run SQL Server integration tests.";
        }
    }
}
