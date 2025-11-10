using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class Addprivacylevelandfollowprivacylevelforprofile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowPrivacyLevel",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrivacyLevel",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowPrivacyLevel",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PrivacyLevel",
                table: "Profiles");
        }
    }
}
