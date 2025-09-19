using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Forum_Attachments_Moderation_Scope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ForumTopics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CondominiumId",
                table: "ForumTopics",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "ForumPosts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ForumPosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ForumCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CondominiumId",
                table: "ForumCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ForumAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumAttachment_ForumPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ForumPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 18, 23, 38, 51, 317, DateTimeKind.Unspecified).AddTicks(401), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_CreatedById",
                table: "ForumPosts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachment_PostId",
                table: "ForumAttachment",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_AspNetUsers_CreatedById",
                table: "ForumPosts",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_AspNetUsers_CreatedById",
                table: "ForumPosts");

            migrationBuilder.DropTable(
                name: "ForumAttachment");

            migrationBuilder.DropIndex(
                name: "IX_ForumPosts_CreatedById",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ForumTopics");

            migrationBuilder.DropColumn(
                name: "CondominiumId",
                table: "ForumTopics");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ForumCategories");

            migrationBuilder.DropColumn(
                name: "CondominiumId",
                table: "ForumCategories");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "ForumPosts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 18, 17, 46, 0, 640, DateTimeKind.Unspecified).AddTicks(5541), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
