using System;
using Microsoft.EntityFrameworkCore.Migrations;

using Favi_BE.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Favi_BE.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423000000_AddUpdatedAtAndVersionToProfile")]
    public partial class AddUpdatedAtAndVersionToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Profiles");
        }
    }
}
