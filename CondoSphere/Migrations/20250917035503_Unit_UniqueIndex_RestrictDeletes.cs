using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Unit_UniqueIndex_RestrictDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotas_Units_UnitId",
                table: "Quotas");

            migrationBuilder.DropIndex(
                name: "IX_Units_CondominiumId",
                table: "Units");

            migrationBuilder.AlterColumn<string>(
                name: "DebtorUserId",
                table: "Quotas",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 17, 3, 55, 2, 866, DateTimeKind.Unspecified).AddTicks(3950), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Units_CondominiumId_Number",
                table: "Units",
                columns: new[] { "CondominiumId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotas_DebtorUserId_DueDate",
                table: "Quotas",
                columns: new[] { "DebtorUserId", "DueDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Quotas_Units_UnitId",
                table: "Quotas",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotas_Units_UnitId",
                table: "Quotas");

            migrationBuilder.DropIndex(
                name: "IX_Units_CondominiumId_Number",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Quotas_DebtorUserId_DueDate",
                table: "Quotas");

            migrationBuilder.AlterColumn<string>(
                name: "DebtorUserId",
                table: "Quotas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 16, 20, 7, 49, 555, DateTimeKind.Unspecified).AddTicks(1819), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Units_CondominiumId",
                table: "Units",
                column: "CondominiumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotas_Units_UnitId",
                table: "Quotas",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
