using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerSupportCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTaskReminderAndMention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceTaskId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTicketNoteId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SourceTaskId",
                table: "Notifications",
                column: "SourceTaskId",
                unique: true,
                filter: "[Type] = 'TaskReminder' AND [SourceTaskId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_SourceTaskId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SourceTaskId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SourceTicketNoteId",
                table: "Notifications");
        }
    }
}
