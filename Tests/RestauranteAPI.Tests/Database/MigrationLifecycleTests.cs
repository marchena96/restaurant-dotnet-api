using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestauranteAPI.Hosting;
using RestauranteAPI.Models;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Implementations;

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
        Assert.Contains("20260920173000_AddV2StatusCatalogsAndBackfill", appliedMigrations);
    }

    [SqlServerIntegrationFact]
    public async Task Legacy_clients_backfill_to_people_and_client_profiles()
    {
        await database.ResetToMigrationAsync("20260920162728_AddIdentityAndRbacV2Foundation");
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
        await database.ExecuteHistoricalSqlAsync($"""
            SET IDENTITY_INSERT [Clients] ON;
            INSERT INTO [Clients] ([Id], [FirstName], [LastName], [PhoneNumber], [IdCard])
            VALUES ({firstClient.Id}, {firstClient.FirstName}, {firstClient.LastName}, {firstClient.PhoneNumber}, {firstClient.IdCard});
            INSERT INTO [Clients] ([Id], [FirstName], [LastName], [PhoneNumber], [IdCard])
            VALUES ({secondClient.Id}, {secondClient.FirstName}, {secondClient.LastName}, {secondClient.PhoneNumber}, {secondClient.IdCard});
            SET IDENTITY_INSERT [Clients] OFF;
            INSERT INTO [Zones] ([Name], [IsAvailable]) VALUES (N'Legacy zone', 1);
            INSERT INTO [Tables] ([TableNumber], [Capacity], [ZoneId]) VALUES (N'L1', 4, 1);
            INSERT INTO [Turns] ([Name], [StartTime], [EndTime], [IsActive]) VALUES (N'Legacy turn', '12:00', '14:00', 1);
            INSERT INTO [Reservations] ([Date], [StartTime], [EndTime], [GuestCount], [CreatedAt], [ClientId], [TableId], [StatusId], [TurnId])
            VALUES ('2026-10-01', '12:00', '13:00', 2, SYSUTCDATETIME(), {firstClient.Id}, 1, 1, 1);
            INSERT INTO [WaitingLists] ([Date], [StartTime], [EndTime], [PartySize], [Status], [PreferredZone], [ClientId])
            VALUES ('2026-10-01', '13:00', '14:00', 3, N'Waiting', NULL, {secondClient.Id});
            """);

        await using var context = database.CreateDbContext();

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
    public async Task Legacy_statuses_backfill_to_v2_catalogs_without_changing_legacy_storage()
    {
        await database.ResetToMigrationAsync("20260920173000_AddV2StatusCatalogsAndBackfill");
        var idCard = $"status-{Guid.NewGuid():N}";
        await database.ExecuteHistoricalSqlAsync($"""
            INSERT INTO [Clients] ([FirstName], [LastName], [PhoneNumber], [IdCard]) VALUES (N'Status', N'Legacy', N'5550103', {idCard});
            INSERT INTO [Zones] ([Name], [IsAvailable]) VALUES (N'Status zone', 1);
            INSERT INTO [Tables] ([TableNumber], [Capacity], [ZoneId]) VALUES (N'S1', 4, 1);
            INSERT INTO [Turns] ([Name], [StartTime], [EndTime], [IsActive]) VALUES (N'Status turn', '12:00', '14:00', 1);
            INSERT INTO [Reservations] ([Date], [StartTime], [EndTime], [GuestCount], [CreatedAt], [ClientId], [TableId], [StatusId], [TurnId]) VALUES ('2026-10-02', '12:00', '13:00', 2, SYSUTCDATETIME(), 1, 1, 1, 1);
            INSERT INTO [WaitingLists] ([Date], [StartTime], [EndTime], [PartySize], [Status], [PreferredZone], [ClientId]) VALUES ('2026-10-02', '13:00', '14:00', 3, N'Assigned', NULL, 1);
            """);
        await using var context = database.CreateDbContext();

        var legacyStatusIds = await context.Reservations
            .Select(item => new { item.Id, item.StatusId })
            .ToDictionaryAsync(item => item.Id, item => item.StatusId);
        var legacyWaitingStatuses = await context.WaitingLists
            .Select(item => new { item.Id, item.Status })
            .ToDictionaryAsync(item => item.Id, item => item.Status);

        await context.Database.MigrateAsync();

        var reservationStatuses = await context.Database
            .SqlQueryRaw<ReservationStatusRow>("SELECT [Code], [Name], [BlocksAvailability], [IsTerminal], [SortOrder], [IsActive] FROM [ReservationStatus]")
            .ToDictionaryAsync(item => item.Code);
        var waitingStatuses = await context.Database
            .SqlQueryRaw<WaitingListStatusRow>("SELECT [Code], [Name], [IsTerminal], [SortOrder], [IsActive] FROM [WaitingListStatus]")
            .ToDictionaryAsync(item => item.Code);

        Assert.Equal(new[] { "ACTIVE", "CANCELLED", "COMPLETED", "PENDING" }, reservationStatuses.Keys.Order());
        Assert.Equal(new[] { "ASSIGNED", "CANCELLED", "WAITING" }, waitingStatuses.Keys.Order());
        Assert.Equal(4, reservationStatuses.Count);
        Assert.Equal(3, waitingStatuses.Count);
        AssertReservationStatus(reservationStatuses["PENDING"], "Pending", true, false, 0);
        AssertReservationStatus(reservationStatuses["ACTIVE"], "Active", true, false, 1);
        AssertReservationStatus(reservationStatuses["COMPLETED"], "Completed", false, true, 2);
        AssertReservationStatus(reservationStatuses["CANCELLED"], "Cancelled", false, true, 3);
        AssertWaitingListStatus(waitingStatuses["WAITING"], "Waiting", false, 0);
        AssertWaitingListStatus(waitingStatuses["ASSIGNED"], "Assigned", true, 1);
        AssertWaitingListStatus(waitingStatuses["CANCELLED"], "Cancelled", true, 2);
        Assert.Equal(4, await context.Statuses.CountAsync());
        Assert.Equal(legacyStatusIds, await context.Reservations
            .Select(item => new { item.Id, item.StatusId })
            .ToDictionaryAsync(item => item.Id, item => item.StatusId));
        Assert.Equal(legacyWaitingStatuses, await context.WaitingLists
            .Select(item => new { item.Id, item.Status })
            .ToDictionaryAsync(item => item.Id, item => item.Status));
        Assert.Empty(await context.People.ToListAsync());
        Assert.Empty(await context.ClientProfiles.ToListAsync());
        Assert.Empty(await context.UserAccounts.ToListAsync());
        Assert.Empty(await context.UserRoles.ToListAsync());
    }

    [SqlServerIntegrationTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unknown_legacy_status_values_abort_catalog_backfill_without_guessing(bool reservationStatus)
    {
        await database.ResetToMigrationAsync("20260920173000_AddV2StatusCatalogsAndBackfill");
        await using var context = database.CreateDbContext();

        if (reservationStatus)
        {
            await database.ExecuteHistoricalSqlAsync($"INSERT INTO [Statuses] ([Name]) VALUES ({"Unexpected"})");
        }
        else
        {
            var client = new Client
            {
                FirstName = "Waiting",
                LastName = "Legacy",
                PhoneNumber = "5550104",
                IdCard = $"waiting-{Guid.NewGuid():N}"
            };
            await database.ExecuteHistoricalSqlAsync($"""
                INSERT INTO [Clients] ([FirstName], [LastName], [PhoneNumber], [IdCard]) VALUES ({client.FirstName}, {client.LastName}, {client.PhoneNumber}, {client.IdCard});
                INSERT INTO [WaitingLists] ([Date], [StartTime], [EndTime], [PartySize], [Status], [PreferredZone], [ClientId]) VALUES ('2026-10-03', '12:00', '13:00', 2, {"Unexpected"}, NULL, 1);
                """);
        }

        var exception = await Record.ExceptionAsync(() => context.Database.MigrateAsync());

        Assert.NotNull(exception);
        Assert.Contains("cannot be mapped", exception.ToString());
        Assert.DoesNotContain(
            "20260920175544_AddV2StatusForeignKeysAndCutover",
            await context.Database.GetAppliedMigrationsAsync());

        if (reservationStatus)
        {
            await context.Database.ExecuteSqlAsync($"DELETE FROM [Statuses] WHERE [Name] = {"Unexpected"}");
        }
        else
        {
            await context.Database.ExecuteSqlAsync($"DELETE FROM [WaitingLists] WHERE [Status] = {"Unexpected"}");
        }

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

    [SqlServerIntegrationFact]
    public async Task Runtime_status_semantics_use_codes_when_catalog_ids_are_renumbered()
    {
        await database.ResetToMigrationAsync("20260920175544_AddV2StatusForeignKeysAndCutover");
        await database.ExecuteHistoricalSqlAsync($"""
            -- {string.Empty}
            DELETE FROM [ReservationStatus];
            DELETE FROM [WaitingListStatus];
            SET IDENTITY_INSERT [ReservationStatus] ON;
            INSERT INTO [ReservationStatus] ([ReservationStatusId], [Code], [Name], [BlocksAvailability], [IsTerminal], [SortOrder], [IsActive]) VALUES
                (91, N'PENDING', N'Pending', 1, 0, 3, 1), (83, N'ACTIVE', N'Active', 1, 0, 0, 1),
                (66, N'CANCELLED', N'Cancelled', 0, 1, 1, 1), (74, N'COMPLETED', N'Completed', 0, 1, 2, 1);
            SET IDENTITY_INSERT [ReservationStatus] OFF;
            SET IDENTITY_INSERT [WaitingListStatus] ON;
            INSERT INTO [WaitingListStatus] ([WaitingListStatusId], [Code], [Name], [IsTerminal], [SortOrder], [IsActive]) VALUES
                (61, N'WAITING', N'Waiting', 0, 2, 1), (52, N'ASSIGNED', N'Assigned', 1, 0, 1), (43, N'CANCELLED', N'Cancelled', 1, 1, 1);
            SET IDENTITY_INSERT [WaitingListStatus] OFF;
            """);
        await using var context = database.CreateDbContext();
        var client = new Client { FirstName = "Runtime", LastName = "Status", PhoneNumber = "5550199", IdCard = $"runtime-{Guid.NewGuid():N}" };
        var zone = new Zone { Name = "Runtime zone", IsAvailable = true };
        var table = new Table { TableNumber = "R1", Capacity = 4, Zone = zone };
        var turn = new Turn { Name = "Runtime turn", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(14, 0), IsActive = true };
        context.AddRange(client, zone, table, turn);
        await context.SaveChangesAsync();

        var reservationService = new ReservationService(context);
        var waitingListService = new WaitingListService(context);
        var request = new CreateReservationRequest { ClientId = client.Id, TableId = table.Id, Date = "2026-11-01", ReservationTime = "12:00", GuestCount = 2 };
        var reservation = await reservationService.CreateAsync(request);
        var storedReservation = await context.Reservations.SingleAsync(item => item.Id == reservation.Id);
        Assert.Equal(91, storedReservation.ReservationStatusId);

        await reservationService.UpdateStatusAsync(reservation.Id, 4);
        Assert.Equal(66, await context.Reservations.Where(item => item.Id == reservation.Id).Select(item => item.ReservationStatusId).SingleAsync());
        Assert.True(await new TableService(context).IsTableAvailableAsync(table.Id, new DateOnly(2026, 11, 1), new TimeOnly(12, 0), new TimeOnly(13, 0)));

        var waiting = await waitingListService.CreateAsync(new WaitingListDto { ClientId = client.Id, Date = new DateOnly(2026, 11, 1), StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(14, 0), PartySize = 2 });
        Assert.Equal(61, await context.WaitingLists.Where(item => item.Id == waiting.Id).Select(item => item.WaitingListStatusId).SingleAsync());
        await waitingListService.UpdateStatusAsync(waiting.Id, "Asignado");
        Assert.Equal(52, await context.WaitingLists.Where(item => item.Id == waiting.Id).Select(item => item.WaitingListStatusId).SingleAsync());
        await waitingListService.UpdateStatusAsync(waiting.Id, "Cancelado");
        Assert.Equal(43, await context.WaitingLists.Where(item => item.Id == waiting.Id).Select(item => item.WaitingListStatusId).SingleAsync());
    }

    [SqlServerIntegrationFact]
    public async Task Missing_required_status_code_fails_without_legacy_fallback()
    {
        await database.ResetToMigrationAsync("20260920175544_AddV2StatusForeignKeysAndCutover");
        await database.ExecuteHistoricalSqlAsync($"DELETE FROM [ReservationStatus] WHERE [Code] = {"PENDING"}");
        await using var context = database.CreateDbContext();
        var client = new Client { FirstName = "Missing", LastName = "Code", PhoneNumber = "5550198", IdCard = $"missing-{Guid.NewGuid():N}" };
        var zone = new Zone { Name = "Missing zone", IsAvailable = true };
        var table = new Table { TableNumber = "M1", Capacity = 4, Zone = zone };
        var turn = new Turn { Name = "Missing turn", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(14, 0), IsActive = true };
        context.AddRange(client, zone, table, turn);
        await context.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(() => new ReservationService(context).CreateAsync(
            new CreateReservationRequest { ClientId = client.Id, TableId = table.Id, Date = "2026-11-02", ReservationTime = "12:00", GuestCount = 2 }));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PENDING", exception.Message);
        Assert.Empty(await context.Reservations.ToListAsync());
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

    private static void AssertReservationStatus(
        ReservationStatusRow status,
        string name,
        bool blocksAvailability,
        bool isTerminal,
        int sortOrder)
    {
        Assert.Equal(name, status.Name);
        Assert.Equal(blocksAvailability, status.BlocksAvailability);
        Assert.Equal(isTerminal, status.IsTerminal);
        Assert.Equal(sortOrder, status.SortOrder);
        Assert.True(status.IsActive);
    }

    private static void AssertWaitingListStatus(
        WaitingListStatusRow status,
        string name,
        bool isTerminal,
        int sortOrder)
    {
        Assert.Equal(name, status.Name);
        Assert.Equal(isTerminal, status.IsTerminal);
        Assert.Equal(sortOrder, status.SortOrder);
        Assert.True(status.IsActive);
    }

    private sealed class ReservationStatusRow
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool BlocksAvailability { get; init; }
        public bool IsTerminal { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class WaitingListStatusRow
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsTerminal { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
    }
}
