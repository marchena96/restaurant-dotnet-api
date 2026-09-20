namespace RestauranteAPI.Tests.Database;

public sealed class SqlServerIntegrationFactAttribute : FactAttribute
{
    public SqlServerIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                SqlServerDatabaseFixture.ConnectionStringEnvironmentVariable)))
        {
            Skip = $"Set {SqlServerDatabaseFixture.ConnectionStringEnvironmentVariable} to run SQL Server integration tests.";
        }
    }
}
