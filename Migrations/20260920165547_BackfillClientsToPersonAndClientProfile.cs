using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestauranteAPI.Migrations
{
    /// <inheritdoc />
    public partial class BackfillClientsToPersonAndClientProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM [Clients] WITH (TABLOCKX, HOLDLOCK)
                    WHERE DATALENGTH([FirstName]) > 200
                       OR DATALENGTH([LastName]) > 200
                       OR DATALENGTH([PhoneNumber]) > 60
                       OR DATALENGTH([IdCard]) > 100
                )
                BEGIN
                    THROW 51000, 'Legacy client data exceeds v2 Person column limits; migration aborted without truncation.', 1;
                END;

                -- One batch-level UTC cutover timestamp; legacy Clients have no historical timestamps.
                DECLARE @MigratedAtUtc datetime2(3) = SYSUTCDATETIME();
                DECLARE @ClientPersonMap TABLE
                (
                    [ClientId] int NOT NULL PRIMARY KEY,
                    [PersonId] int NOT NULL
                );

                MERGE [People] AS [target]
                USING [Clients] AS [source]
                    ON 1 = 0
                WHEN NOT MATCHED THEN
                    INSERT
                    (
                        [IdentificationNumber],
                        [FirstName],
                        [LastName],
                        [Email],
                        [PhoneNumber],
                        [IsActive],
                        [CreatedAtUtc],
                        [UpdatedAtUtc]
                    )
                    VALUES
                    (
                        [source].[IdCard],
                        [source].[FirstName],
                        [source].[LastName],
                        NULL,
                        [source].[PhoneNumber],
                        1,
                        @MigratedAtUtc,
                        @MigratedAtUtc
                    )
                OUTPUT [source].[Id], [inserted].[PersonId]
                    INTO @ClientPersonMap ([ClientId], [PersonId]);

                SET IDENTITY_INSERT [ClientProfiles] ON;

                INSERT INTO [ClientProfiles]
                (
                    [ClientId],
                    [PersonId],
                    [IsActive],
                    [CustomerSinceUtc],
                    [Notes],
                    [UpdatedAtUtc]
                )
                SELECT
                    [map].[ClientId],
                    [map].[PersonId],
                    1,
                    @MigratedAtUtc,
                    NULL,
                    @MigratedAtUtc
                FROM @ClientPersonMap AS [map];

                SET IDENTITY_INSERT [ClientProfiles] OFF;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "THROW 51001, 'BackfillClientsToPersonAndClientProfile cannot be reversed without deleting migrated v2 data.', 1;");
        }
    }
}
