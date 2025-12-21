using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class addLocationAndPeopleTagToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationFullAddress",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LocationLatitude",
                table: "Posts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LocationLongitude",
                table: "Posts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Posts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationFullAddress",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Posts");
        }
    }
}
