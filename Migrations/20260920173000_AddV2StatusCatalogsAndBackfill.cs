using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestauranteAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddV2StatusCatalogsAndBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM [Statuses] WITH (TABLOCKX, HOLDLOCK)
                    WHERE [Name] NOT IN (N'Pending', N'Active', N'Completed', N'Cancelled')
                )
                BEGIN
                    THROW 51002, 'Legacy reservation status name is not recognized; migration aborted without guessing semantics.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM [WaitingLists] WITH (TABLOCKX, HOLDLOCK)
                    WHERE [Status] NOT IN (N'Waiting', N'Assigned', N'Cancelled')
                )
                BEGIN
                    THROW 51003, 'Legacy waiting-list status is not recognized; migration aborted without guessing semantics.', 1;
                END;
                """);

            migrationBuilder.CreateTable(
                name: "ReservationStatus",
                columns: table => new
                {
                    ReservationStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlocksAvailability = table.Column<bool>(type: "bit", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationStatus", x => x.ReservationStatusId);
                    table.CheckConstraint("CK_ReservationStatus_SortOrder", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "WaitingListStatus",
                columns: table => new
                {
                    WaitingListStatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingListStatus", x => x.WaitingListStatusId);
                    table.CheckConstraint("CK_WaitingListStatus_SortOrder", "[SortOrder] >= 0");
                });

            migrationBuilder.Sql("""
                INSERT INTO [ReservationStatus]
                    ([Code], [Name], [BlocksAvailability], [IsTerminal], [SortOrder], [IsActive])
                VALUES
                    (N'PENDING', N'Pending', 1, 0, 0, 1),
                    (N'ACTIVE', N'Active', 1, 0, 1, 1),
                    (N'COMPLETED', N'Completed', 0, 1, 2, 1),
                    (N'CANCELLED', N'Cancelled', 0, 1, 3, 1);

                INSERT INTO [WaitingListStatus]
                    ([Code], [Name], [IsTerminal], [SortOrder], [IsActive])
                VALUES
                    (N'WAITING', N'Waiting', 0, 0, 1),
                    (N'ASSIGNED', N'Assigned', 1, 1, 1),
                    (N'CANCELLED', N'Cancelled', 1, 2, 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatus_Code",
                table: "ReservationStatus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListStatus_Code",
                table: "WaitingListStatus",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReservationStatus");
            migrationBuilder.DropTable(name: "WaitingListStatus");
        }
    }
}
