using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Quota_DebtorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DebtorEmail",
                table: "Quotas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebtorName",
                table: "Quotas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebtorUserId",
                table: "Quotas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 16, 20, 7, 49, 555, DateTimeKind.Unspecified).AddTicks(1819), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtorEmail",
                table: "Quotas");

            migrationBuilder.DropColumn(
                name: "DebtorName",
                table: "Quotas");

            migrationBuilder.DropColumn(
                name: "DebtorUserId",
                table: "Quotas");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 16, 18, 12, 54, 556, DateTimeKind.Unspecified).AddTicks(95), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
