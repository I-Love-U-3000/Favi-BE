using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAccountForLocalAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccounts", x => x.Id);
                    table.UniqueConstraint("AK_EmailAccounts_Email", x => x.Email);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Id",
                table: "EmailAccounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailAccounts_Profiles_Id",
                table: "EmailAccounts",
                column: "Id",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailAccounts_Profiles_Id",
                table: "EmailAccounts");

            migrationBuilder.DropTable(
                name: "EmailAccounts");
        }
    }
}
