using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class MeetingtV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Meetings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OnlineJoinUrl",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineMeetingId",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineProvider",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineStartUrl",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "OnlineJoinUrl",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "OnlineMeetingId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "OnlineProvider",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "OnlineStartUrl",
                table: "Meetings");
        }
    }
}
