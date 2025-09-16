using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReceiptEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PaymentReceiptEmailEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptEmailHtml",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptEmailSubject",
                table: "SystemSettings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PaymentReceiptEmailEnabled", "PaymentReceiptEmailHtml", "PaymentReceiptEmailSubject", "UpdatedAt" },
                values: new object[] { true, null, null, new DateTimeOffset(new DateTime(2025, 9, 16, 17, 12, 12, 200, DateTimeKind.Unspecified).AddTicks(5233), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentReceiptEmailEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptEmailHtml",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptEmailSubject",
                table: "SystemSettings");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 16, 16, 41, 49, 491, DateTimeKind.Unspecified).AddTicks(8451), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
