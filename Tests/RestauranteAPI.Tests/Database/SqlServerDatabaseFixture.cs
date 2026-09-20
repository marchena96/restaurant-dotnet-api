using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;

namespace RestauranteAPI.Tests.Database;

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable =
        "RESTAURANT_TEST_SQLSERVER_CONNECTION_STRING";

    private readonly string _databaseName = $"RestaurantTests_{Guid.NewGuid():N}";
    private string? _masterConnectionString;

    public string DatabaseConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to a SQL Server connection string before running integration tests.");
        }

        var masterBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = masterBuilder.ConnectionString;

        var databaseBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = _databaseName
        };
        DatabaseConnectionString = databaseBuilder.ConnectionString;

        await using var masterConnection = new SqlConnection(_masterConnectionString);
        await masterConnection.OpenAsync();
        await using (var createCommand = masterConnection.CreateCommand())
        {
            createCommand.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await createCommand.ExecuteNonQueryAsync();
        }

        try
        {
            await using var context = CreateDbContext();
            await context.Database.MigrateAsync();
        }
        catch
        {
            await DropDatabaseAsync();
            throw;
        }
    }

    public Task DisposeAsync() => DropDatabaseAsync();

    public MyAppDbContext CreateDbContext()
    {
        if (string.IsNullOrWhiteSpace(DatabaseConnectionString))
        {
            throw new InvalidOperationException("Integration database is not initialized.");
        }

        var options = new DbContextOptionsBuilder<MyAppDbContext>()
            .UseSqlServer(DatabaseConnectionString)
            .Options;

        return new MyAppDbContext(options);
    }

    public async Task ResetToMigrationAsync(string migration)
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync(migration);
    }

    public async Task ExecuteHistoricalSqlAsync(FormattableString sql)
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlAsync(sql);
    }

    private async Task DropDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(_masterConnectionString))
        {
            return;
        }

        await using var masterConnection = new SqlConnection(_masterConnectionString);
        await masterConnection.OpenAsync();
        await using var dropCommand = masterConnection.CreateCommand();
        dropCommand.CommandText = $"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;
        await dropCommand.ExecuteNonQueryAsync();
    }
}
