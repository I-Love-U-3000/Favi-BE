using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "Reactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_CollectionId_ProfileId",
                table: "Reactions",
                columns: new[] { "CollectionId", "ProfileId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Collections_CollectionId",
                table: "Reactions",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Collections_CollectionId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_CollectionId_ProfileId",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "Reactions");
        }
    }
}
