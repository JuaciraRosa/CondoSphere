using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerEmailToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerEmail",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChatThreads",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 23, 15, 41, 58, 28, DateTimeKind.Unspecified).AddTicks(4413), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_ResidentId_Status",
                table: "ChatThreads",
                columns: new[] { "ResidentId", "Status" },
                unique: true,
                filter: "[Status] = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatThreads_ResidentId_Status",
                table: "ChatThreads");

            migrationBuilder.DropColumn(
                name: "PayerEmail",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChatThreads",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 19, 8, 55, 56, 922, DateTimeKind.Unspecified).AddTicks(1202), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
