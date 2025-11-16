using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddReactionForComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PostId",
                table: "Reactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Reactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CommentId",
                table: "Reactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_CommentId_ProfileId",
                table: "Reactions",
                columns: new[] { "CommentId", "ProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId_ProfileId",
                table: "Reactions",
                columns: new[] { "PostId", "ProfileId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Comments_CommentId",
                table: "Reactions",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Comments_CommentId",
                table: "Reactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_CommentId_ProfileId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_PostId_ProfileId",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Reactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PostId",
                table: "Reactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reactions",
                table: "Reactions",
                columns: new[] { "PostId", "ProfileId" });
        }
    }
}
