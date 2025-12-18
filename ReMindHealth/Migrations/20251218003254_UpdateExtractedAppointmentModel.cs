using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReMindHealth.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExtractedAppointmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAddedToCalendar",
                table: "ExtractedAppointments");

            migrationBuilder.DropColumn(
                name: "IsAllDay",
                table: "ExtractedAppointments");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "ExtractedAppointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConversationId",
                table: "ExtractedAppointments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Add UserId column as NULLABLE first
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ExtractedAppointments",
                type: "text",
                nullable: true);  // Changed to nullable temporarily

            // Populate UserId from related Conversations
            migrationBuilder.Sql(@"
        UPDATE ""ExtractedAppointments"" ea
        SET ""UserId"" = c.""UserId""
        FROM ""Conversations"" c
        WHERE ea.""ConversationId"" = c.""ConversationId""
        AND ea.""ConversationId"" IS NOT NULL;
    ");

            // Delete orphaned appointments that couldn't get a UserId
            migrationBuilder.Sql(@"
        DELETE FROM ""ExtractedAppointments""
        WHERE ""UserId"" IS NULL;
    ");

            // Now make UserId NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ExtractedAppointments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Create index and foreign key
            migrationBuilder.CreateIndex(
                name: "IX_ExtractedAppointments_UserId",
                table: "ExtractedAppointments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedAppointments_AspNetUsers_UserId",
                table: "ExtractedAppointments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedAppointments_AspNetUsers_UserId",
                table: "ExtractedAppointments");

            migrationBuilder.DropIndex(
                name: "IX_ExtractedAppointments_UserId",
                table: "ExtractedAppointments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ExtractedAppointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConversationId",
                table: "ExtractedAppointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAddedToCalendar",
                table: "ExtractedAppointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAllDay",
                table: "ExtractedAppointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "ExtractedAppointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
