using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class TerceraMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Turns_TurnId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Turns_Availabilities_AvailabilityId",
                table: "Turns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Turns",
                table: "Turns");

            migrationBuilder.RenameTable(
                name: "Turns",
                newName: "Availability Slots");

            migrationBuilder.RenameIndex(
                name: "IX_Turns_AvailabilityId",
                table: "Availability Slots",
                newName: "IX_Availability Slots_AvailabilityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Availability Slots",
                table: "Availability Slots",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Availability Slots_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "Availability Slots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Availability Slots_Availabilities_AvailabilityId",
                table: "Availability Slots",
                column: "AvailabilityId",
                principalTable: "Availabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Availability Slots_TurnId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Availability Slots_Availabilities_AvailabilityId",
                table: "Availability Slots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Availability Slots",
                table: "Availability Slots");

            migrationBuilder.RenameTable(
                name: "Availability Slots",
                newName: "Turns");

            migrationBuilder.RenameIndex(
                name: "IX_Availability Slots_AvailabilityId",
                table: "Turns",
                newName: "IX_Turns_AvailabilityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Turns",
                table: "Turns",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Turns_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "Turns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turns_Availabilities_AvailabilityId",
                table: "Turns",
                column: "AvailabilityId",
                principalTable: "Availabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
