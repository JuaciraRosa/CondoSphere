using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Forum_Attachments_Moderation_Scope_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumSubscriptions_ForumTopics_ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropTable(
                name: "ForumAttachment");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_TopicId_UserId",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ForumSubscriptions");

            migrationBuilder.RenameColumn(
                name: "TopicId",
                table: "ForumSubscriptions",
                newName: "PostId");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ForumSubscriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ForumSubscriptions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "ForumSubscriptions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "ForumSubscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ForumAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForumTopicId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumAttachments_ForumTopics_ForumTopicId",
                        column: x => x.ForumTopicId,
                        principalTable: "ForumTopics",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 19, 0, 47, 36, 775, DateTimeKind.Unspecified).AddTicks(9930), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ForumSubscriptions_PostId",
                table: "ForumSubscriptions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_ForumTopicId",
                table: "ForumAttachments",
                column: "ForumTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_TopicId_UserId",
                table: "ForumAttachments",
                columns: new[] { "TopicId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumSubscriptions_ForumPosts_PostId",
                table: "ForumSubscriptions",
                column: "PostId",
                principalTable: "ForumPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumSubscriptions_ForumPosts_PostId",
                table: "ForumSubscriptions");

            migrationBuilder.DropTable(
                name: "ForumAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_PostId",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "ForumSubscriptions");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "ForumSubscriptions",
                newName: "TopicId");

            migrationBuilder.AddColumn<int>(
                name: "ForumTopicId",
                table: "ForumSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ForumSubscriptions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ForumAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false)
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
                name: "IX_ForumSubscriptions_ForumTopicId",
                table: "ForumSubscriptions",
                column: "ForumTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumSubscriptions_TopicId_UserId",
                table: "ForumSubscriptions",
                columns: new[] { "TopicId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachment_PostId",
                table: "ForumAttachment",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumSubscriptions_ForumTopics_ForumTopicId",
                table: "ForumSubscriptions",
                column: "ForumTopicId",
                principalTable: "ForumTopics",
                principalColumn: "Id");
        }
    }
}
