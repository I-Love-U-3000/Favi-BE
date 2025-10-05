using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class seconddbscaffoldaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Profiles_ReporterId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "Privacy",
                table: "Collections",
                newName: "PrivacyLevel");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReporterId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "PostMedias",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "PostMedias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "PostMedias",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "PostMedias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Profiles_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Profiles_ReporterId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "PostMedias");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "PostMedias");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "PostMedias");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "PostMedias");

            migrationBuilder.RenameColumn(
                name: "PrivacyLevel",
                table: "Collections",
                newName: "Privacy");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReporterId",
                table: "Reports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Profiles_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "Profiles",
                principalColumn: "Id");
        }
    }
}
