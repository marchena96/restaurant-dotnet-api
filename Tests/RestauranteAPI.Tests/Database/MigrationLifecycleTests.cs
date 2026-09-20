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
        Assert.Contains("20260920165547_BackfillClientsToPersonAndClientProfile", appliedMigrations);
    }

    [SqlServerIntegrationFact]
    public async Task Legacy_clients_backfill_to_people_and_client_profiles()
    {
        await database.ResetToMigrationAsync("20260920162728_AddIdentityAndRbacV2Foundation");
        await using var context = database.CreateDbContext();

        var firstClient = new Client
        {
            Id = 41,
            FirstName = "Legacy",
            LastName = "One",
            PhoneNumber = "5550100",
            IdCard = $"legacy-{Guid.NewGuid():N}"
        };
        var secondClient = new Client
        {
            Id = 87,
            FirstName = "Legacy",
            LastName = "Two",
            PhoneNumber = "5550101",
            IdCard = $"legacy-{Guid.NewGuid():N}"
        };
        await context.Database.ExecuteSqlAsync($"""
            SET IDENTITY_INSERT [Clients] ON;
            INSERT INTO [Clients] ([Id], [FirstName], [LastName], [PhoneNumber], [IdCard])
            VALUES ({firstClient.Id}, {firstClient.FirstName}, {firstClient.LastName}, {firstClient.PhoneNumber}, {firstClient.IdCard});
            INSERT INTO [Clients] ([Id], [FirstName], [LastName], [PhoneNumber], [IdCard])
            VALUES ({secondClient.Id}, {secondClient.FirstName}, {secondClient.LastName}, {secondClient.PhoneNumber}, {secondClient.IdCard});
            SET IDENTITY_INSERT [Clients] OFF;
            """);

        var zone = new Zone { Name = "Legacy zone", IsAvailable = true };
        var table = new Table { TableNumber = "L1", Capacity = 4, Zone = zone };
        var turn = new Turn
        {
            Name = "Legacy turn",
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(14, 0),
            IsActive = true
        };
        context.AddRange(zone, table, turn);
        await context.SaveChangesAsync();

        context.Reservations.Add(new Reservation
        {
            ClientId = firstClient.Id,
            TableId = table.Id,
            StatusId = 1,
            TurnId = turn.Id,
            Date = new DateOnly(2026, 10, 1),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(13, 0),
            GuestCount = 2
        });
        context.WaitingLists.Add(new WaitingListEntry
        {
            ClientId = secondClient.Id,
            Date = new DateOnly(2026, 10, 1),
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(14, 0),
            PartySize = 3
        });
        await context.SaveChangesAsync();

        Assert.DoesNotContain(
            "20260920165547_BackfillClientsToPersonAndClientProfile",
            await context.Database.GetAppliedMigrationsAsync());

        await context.Database.MigrateAsync();

        Assert.Contains(
            "20260920165547_BackfillClientsToPersonAndClientProfile",
            await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(2, await context.Clients.CountAsync());
        Assert.Equal(2, await context.People.CountAsync());
        Assert.Equal(2, await context.ClientProfiles.CountAsync());
        Assert.Equal(1, await context.Reservations.CountAsync());
        Assert.Equal(1, await context.WaitingLists.CountAsync());
        Assert.Equal(0, await context.UserAccounts.CountAsync());
        Assert.Equal(0, await context.UserRoles.CountAsync());

        var profiles = await context.ClientProfiles
            .Include(profile => profile.Person)
            .OrderBy(profile => profile.ClientId)
            .ToListAsync();

        AssertProfile(profiles, firstClient);
        AssertProfile(profiles, secondClient);
        Assert.NotEqual(profiles[0].PersonId, profiles[1].PersonId);
        Assert.Equal(firstClient.Id, await context.Reservations.Select(reservation => reservation.ClientId).SingleAsync());
        Assert.Equal(secondClient.Id, await context.WaitingLists.Select(entry => entry.ClientId).SingleAsync());
    }

    [SqlServerIntegrationTheory]
    [InlineData(nameof(Client.FirstName), 101)]
    [InlineData(nameof(Client.LastName), 101)]
    [InlineData(nameof(Client.PhoneNumber), 31)]
    [InlineData(nameof(Client.IdCard), 51)]
    public async Task Invalid_legacy_client_lengths_fail_the_backfill_without_truncation(
        string propertyName,
        int length)
    {
        await database.ResetToMigrationAsync("20260920162728_AddIdentityAndRbacV2Foundation");
        await using var context = database.CreateDbContext();
        var invalidClient = new Client
        {
            FirstName = "Valid",
            LastName = "Valid",
            PhoneNumber = "5550102",
            IdCard = $"invalid-{Guid.NewGuid():N}"
        };
        typeof(Client).GetProperty(propertyName)!.SetValue(invalidClient, new string('A', length));
        context.Clients.Add(invalidClient);
        await context.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(() => context.Database.MigrateAsync());

        Assert.NotNull(exception);
        Assert.Contains("exceeds v2 Person column limits", exception.ToString());
        Assert.Empty(await context.People.ToListAsync());
        Assert.Empty(await context.ClientProfiles.ToListAsync());
        Assert.DoesNotContain(
            "20260920165547_BackfillClientsToPersonAndClientProfile",
            await context.Database.GetAppliedMigrationsAsync());

        context.Clients.Remove(invalidClient);
        await context.SaveChangesAsync();
        await context.Database.MigrateAsync();
    }

    [SqlServerIntegrationFact]
    public async Task Explicit_migration_mode_succeeds_for_an_up_to_date_database()
    {
        await using var context = database.CreateDbContext();

        var exitCode = await MigrationMode.RunAsync(context, NullLogger.Instance);

        Assert.Equal(0, exitCode);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    private static void AssertProfile(IReadOnlyCollection<ClientProfile> profiles, Client client)
    {
        var profile = Assert.Single(profiles, profile => profile.ClientId == client.Id);

        Assert.Equal(client.Id, profile.ClientId);
        Assert.Equal(client.FirstName, profile.Person.FirstName);
        Assert.Equal(client.LastName, profile.Person.LastName);
        Assert.Equal(client.PhoneNumber, profile.Person.PhoneNumber);
        Assert.Equal(client.IdCard, profile.Person.IdentificationNumber);
        Assert.Null(profile.Person.Email);
        Assert.True(profile.Person.IsActive);
        Assert.True(profile.IsActive);
        Assert.Null(profile.Notes);
    }
}
