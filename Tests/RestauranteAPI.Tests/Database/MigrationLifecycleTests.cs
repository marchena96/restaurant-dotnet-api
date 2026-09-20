using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestauranteAPI.Hosting;

namespace RestauranteAPI.Tests.Database;

[Trait("Category", "Integration")]
public sealed class MigrationLifecycleTests(SqlServerDatabaseFixture database)
    : IClassFixture<SqlServerDatabaseFixture>
{
    [SqlServerIntegrationFact]
    public async Task Existing_migrations_initialize_an_isolated_database()
    {
        await using var context = database.CreateDbContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.NotEmpty(appliedMigrations);
        Assert.Contains("20260608145656_InitialCreate", appliedMigrations);
    }

    [SqlServerIntegrationFact]
    public async Task Explicit_migration_mode_succeeds_for_an_up_to_date_database()
    {
        await using var context = database.CreateDbContext();

        var exitCode = await MigrationMode.RunAsync(context, NullLogger.Instance);

        Assert.Equal(0, exitCode);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
