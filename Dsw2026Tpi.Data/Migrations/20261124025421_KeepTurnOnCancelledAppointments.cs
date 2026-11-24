using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class KeepTurnOnCancelledAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TurnId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TurnId",
                table: "Appointments",
                column: "TurnId",
                unique: true,
                filter: "[TurnId] IS NOT NULL AND [Status] <> 'CANCELLED'");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "AvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TurnId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TurnId",
                table: "Appointments",
                column: "TurnId",
                unique: true,
                filter: "[TurnId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "AvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
