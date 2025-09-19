using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class Forum_Attachments_UpdateName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumAttachments_ForumTopics_ForumTopicId",
                table: "ForumAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumSubscriptions_ForumPosts_PostId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_PostId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumAttachments_ForumTopicId",
                table: "ForumAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ForumAttachments_TopicId_UserId",
                table: "ForumAttachments");

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

            migrationBuilder.DropColumn(
                name: "ForumTopicId",
                table: "ForumAttachments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ForumAttachments");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "ForumSubscriptions",
                newName: "TopicId");

            migrationBuilder.RenameColumn(
                name: "TopicId",
                table: "ForumAttachments",
                newName: "PostId");

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

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ForumAttachments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ForumAttachments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "ForumAttachments",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "ForumAttachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 19, 1, 1, 30, 698, DateTimeKind.Unspecified).AddTicks(5384), new TimeSpan(0, 0, 0, 0, 0)));

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
                name: "IX_ForumAttachments_PostId",
                table: "ForumAttachments",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumAttachments_ForumPosts_PostId",
                table: "ForumAttachments",
                column: "PostId",
                principalTable: "ForumPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumSubscriptions_ForumTopics_ForumTopicId",
                table: "ForumSubscriptions",
                column: "ForumTopicId",
                principalTable: "ForumTopics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumAttachments_ForumPosts_PostId",
                table: "ForumAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumSubscriptions_ForumTopics_ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumSubscriptions_TopicId_UserId",
                table: "ForumSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ForumAttachments_PostId",
                table: "ForumAttachments");

            migrationBuilder.DropColumn(
                name: "ForumTopicId",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ForumSubscriptions");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ForumAttachments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ForumAttachments");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "ForumAttachments");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "ForumAttachments");

            migrationBuilder.RenameColumn(
                name: "TopicId",
                table: "ForumSubscriptions",
                newName: "PostId");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "ForumAttachments",
                newName: "TopicId");

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

            migrationBuilder.AddColumn<int>(
                name: "ForumTopicId",
                table: "ForumAttachments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ForumAttachments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

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
                name: "FK_ForumAttachments_ForumTopics_ForumTopicId",
                table: "ForumAttachments",
                column: "ForumTopicId",
                principalTable: "ForumTopics",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumSubscriptions_ForumPosts_PostId",
                table: "ForumSubscriptions",
                column: "PostId",
                principalTable: "ForumPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
