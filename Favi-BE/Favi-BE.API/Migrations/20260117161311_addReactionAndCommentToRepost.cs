using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class addReactionAndCommentToRepost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RepostId",
                table: "Reactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepostId",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_RepostId",
                table: "Reactions",
                column: "RepostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_RepostId",
                table: "Comments",
                column: "RepostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Reposts_RepostId",
                table: "Comments",
                column: "RepostId",
                principalTable: "Reposts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Reposts_RepostId",
                table: "Reactions",
                column: "RepostId",
                principalTable: "Reposts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Reposts_RepostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Reposts_RepostId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_RepostId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Comments_RepostId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "RepostId",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "RepostId",
                table: "Comments");
        }
    }
}
