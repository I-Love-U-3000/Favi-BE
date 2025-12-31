using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddNSFWToPostAndStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNSFW",
                table: "Stories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNSFW",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNSFW",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "IsNSFW",
                table: "Posts");
        }
    }
}
