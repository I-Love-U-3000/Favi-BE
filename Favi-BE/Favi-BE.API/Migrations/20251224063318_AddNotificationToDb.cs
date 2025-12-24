using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RecipientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Comments_TargetCommentId",
                        column: x => x.TargetCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Posts_TargetPostId",
                        column: x => x.TargetPostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Profiles_ActorProfileId",
                        column: x => x.ActorProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Profiles_RecipientProfileId",
                        column: x => x.RecipientProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorProfileId",
                table: "Notifications",
                column: "ActorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientProfileId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TargetCommentId",
                table: "Notifications",
                column: "TargetCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TargetPostId",
                table: "Notifications",
                column: "TargetPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
