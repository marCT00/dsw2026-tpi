using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class rename_tables_names : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Availability Slots_TurnId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Availabilities_Doctors_DoctorId",
                table: "Availabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Availability Slots_Availabilities_AvailabilityId",
                table: "Availability Slots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Availability Slots",
                table: "Availability Slots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Availabilities",
                table: "Availabilities");

            migrationBuilder.RenameTable(
                name: "Availability Slots",
                newName: "AvailabilitySlots");

            migrationBuilder.RenameTable(
                name: "Availabilities",
                newName: "AvailabilityRules");

            migrationBuilder.RenameIndex(
                name: "IX_Availability Slots_AvailabilityId",
                table: "AvailabilitySlots",
                newName: "IX_AvailabilitySlots_AvailabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_Availabilities_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "AvailabilityRules",
                newName: "IX_AvailabilityRules_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilitySlots",
                table: "AvailabilitySlots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvailabilityRules",
                table: "AvailabilityRules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "AvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityRules_Doctors_DoctorId",
                table: "AvailabilityRules",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityId",
                table: "AvailabilitySlots",
                column: "AvailabilityId",
                principalTable: "AvailabilityRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AvailabilitySlots_TurnId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityRules_Doctors_DoctorId",
                table: "AvailabilityRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilitySlots_AvailabilityRules_AvailabilityId",
                table: "AvailabilitySlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilitySlots",
                table: "AvailabilitySlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvailabilityRules",
                table: "AvailabilityRules");

            migrationBuilder.RenameTable(
                name: "AvailabilitySlots",
                newName: "Availability Slots");

            migrationBuilder.RenameTable(
                name: "AvailabilityRules",
                newName: "Availabilities");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilitySlots_AvailabilityId",
                table: "Availability Slots",
                newName: "IX_Availability Slots_AvailabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_AvailabilityRules_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime",
                table: "Availabilities",
                newName: "IX_Availabilities_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Availability Slots",
                table: "Availability Slots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Availabilities",
                table: "Availabilities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Availability Slots_TurnId",
                table: "Appointments",
                column: "TurnId",
                principalTable: "Availability Slots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Availabilities_Doctors_DoctorId",
                table: "Availabilities",
                column: "DoctorId",
                principalTable: "Doctors",
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
    }
}
