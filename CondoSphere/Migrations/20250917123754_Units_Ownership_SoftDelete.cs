using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Units_Ownership_SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnitOwnership_AspNetUsers_OwnerId",
                table: "UnitOwnership");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitOwnership_Units_UnitId",
                table: "UnitOwnership");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitOwnership",
                table: "UnitOwnership");

            migrationBuilder.DropIndex(
                name: "IX_UnitOwnership_UnitId_StartAt",
                table: "UnitOwnership");

            migrationBuilder.RenameTable(
                name: "UnitOwnership",
                newName: "UnitOwnerships");

            migrationBuilder.RenameIndex(
                name: "IX_UnitOwnership_OwnerId",
                table: "UnitOwnerships",
                newName: "IX_UnitOwnerships_OwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitOwnerships",
                table: "UnitOwnerships",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 17, 12, 37, 53, 268, DateTimeKind.Unspecified).AddTicks(6692), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwnerships_UnitId",
                table: "UnitOwnerships",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitOwnerships_AspNetUsers_OwnerId",
                table: "UnitOwnerships",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitOwnerships_Units_UnitId",
                table: "UnitOwnerships",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnitOwnerships_AspNetUsers_OwnerId",
                table: "UnitOwnerships");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitOwnerships_Units_UnitId",
                table: "UnitOwnerships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitOwnerships",
                table: "UnitOwnerships");

            migrationBuilder.DropIndex(
                name: "IX_UnitOwnerships_UnitId",
                table: "UnitOwnerships");

            migrationBuilder.RenameTable(
                name: "UnitOwnerships",
                newName: "UnitOwnership");

            migrationBuilder.RenameIndex(
                name: "IX_UnitOwnerships_OwnerId",
                table: "UnitOwnership",
                newName: "IX_UnitOwnership_OwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitOwnership",
                table: "UnitOwnership",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 17, 4, 34, 46, 321, DateTimeKind.Unspecified).AddTicks(2027), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwnership_UnitId_StartAt",
                table: "UnitOwnership",
                columns: new[] { "UnitId", "StartAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_UnitOwnership_AspNetUsers_OwnerId",
                table: "UnitOwnership",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitOwnership_Units_UnitId",
                table: "UnitOwnership",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
