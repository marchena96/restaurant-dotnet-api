using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestauranteAPI.Migrations
{
    public partial class AddV2StatusForeignKeysAndCutover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "WaitingListStatusId", table: "WaitingLists", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ReservationStatusId", table: "Reservations", type: "int", nullable: true);
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Statuses] WHERE [Name] NOT IN (N'Pending', N'Active', N'Completed', N'Cancelled'))
                    THROW 51004, 'Legacy reservation status cannot be mapped to a v2 status code.', 1;
                IF EXISTS (SELECT 1 FROM [WaitingLists] WHERE [Status] NOT IN (N'Waiting', N'Assigned', N'Cancelled'))
                    THROW 51005, 'Legacy waiting-list status cannot be mapped to a v2 status code.', 1;
                UPDATE r SET [ReservationStatusId] = rs.[ReservationStatusId]
                FROM [Reservations] r
                JOIN [Statuses] s ON s.[Id] = r.[StatusId]
                JOIN [ReservationStatus] rs ON rs.[Code] = CASE s.[Name]
                    WHEN N'Pending' THEN N'PENDING' WHEN N'Active' THEN N'ACTIVE'
                    WHEN N'Completed' THEN N'COMPLETED' WHEN N'Cancelled' THEN N'CANCELLED' END;
                UPDATE w SET [WaitingListStatusId] = ws.[WaitingListStatusId]
                FROM [WaitingLists] w
                JOIN [WaitingListStatus] ws ON ws.[Code] = UPPER(w.[Status]);
                IF EXISTS (SELECT 1 FROM [Reservations] WHERE [ReservationStatusId] IS NULL)
                    THROW 51004, 'Legacy reservation status cannot be mapped to a v2 status code.', 1;
                IF EXISTS (SELECT 1 FROM [WaitingLists] WHERE [WaitingListStatusId] IS NULL)
                    THROW 51005, 'Legacy waiting-list status cannot be mapped to a v2 status code.', 1;
                """);
            migrationBuilder.AlterColumn<int>(name: "WaitingListStatusId", table: "WaitingLists", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "ReservationStatusId", table: "Reservations", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
            migrationBuilder.CreateIndex(name: "IX_WaitingLists_WaitingListStatusId", table: "WaitingLists", column: "WaitingListStatusId");
            migrationBuilder.CreateIndex(name: "IX_Reservations_ReservationStatusId", table: "Reservations", column: "ReservationStatusId");
            migrationBuilder.AddForeignKey(name: "FK_Reservations_ReservationStatus_ReservationStatusId", table: "Reservations", column: "ReservationStatusId", principalTable: "ReservationStatus", principalColumn: "ReservationStatusId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_WaitingLists_WaitingListStatus_WaitingListStatusId", table: "WaitingLists", column: "WaitingListStatusId", principalTable: "WaitingListStatus", principalColumn: "WaitingListStatusId", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Reservations_ReservationStatus_ReservationStatusId", table: "Reservations");
            migrationBuilder.DropForeignKey(name: "FK_WaitingLists_WaitingListStatus_WaitingListStatusId", table: "WaitingLists");
            migrationBuilder.DropIndex(name: "IX_WaitingLists_WaitingListStatusId", table: "WaitingLists");
            migrationBuilder.DropIndex(name: "IX_Reservations_ReservationStatusId", table: "Reservations");
            migrationBuilder.DropColumn(name: "WaitingListStatusId", table: "WaitingLists");
            migrationBuilder.DropColumn(name: "ReservationStatusId", table: "Reservations");
        }
    }
}
