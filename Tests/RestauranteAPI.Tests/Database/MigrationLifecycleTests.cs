using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestauranteAPI.Hosting;
using RestauranteAPI.Models;

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
        Assert.Contains("20260920162728_AddIdentityAndRbacV2Foundation", appliedMigrations);
    }

    [SqlServerIntegrationFact]
    public async Task Current_identity_rbac_migration_applies_after_legacy_schema()
    {
        await using var context = database.CreateDbContext();

        await context.Database.MigrateAsync("20260608145656_InitialCreate");
        Assert.Contains(
            "20260608145656_InitialCreate",
            await context.Database.GetAppliedMigrationsAsync());
        Assert.DoesNotContain(
            "20260920162728_AddIdentityAndRbacV2Foundation",
            await context.Database.GetAppliedMigrationsAsync());

        var legacyClient = new Client
        {
            FirstName = "Legacy",
            LastName = "Client",
            PhoneNumber = "5550100",
            IdCard = $"legacy-{Guid.NewGuid():N}"
        };
        context.Clients.Add(legacyClient);
        await context.SaveChangesAsync();

        await context.Database.MigrateAsync();

        Assert.Contains(
            "20260920162728_AddIdentityAndRbacV2Foundation",
            await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal("Legacy", await context.Clients
            .Where(client => client.Id == legacyClient.Id)
            .Select(client => client.FirstName)
            .SingleAsync());
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
