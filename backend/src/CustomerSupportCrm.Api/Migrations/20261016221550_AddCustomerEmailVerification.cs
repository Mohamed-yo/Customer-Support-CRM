using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerSupportCrm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAtUtc",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "Customers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_EmailVerificationTokenHash",
                table: "Customers",
                column: "EmailVerificationTokenHash",
                unique: true,
                filter: "[EmailVerificationTokenHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_EmailVerificationTokenHash",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAtUtc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "Customers");
        }
    }
}
