using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favi_BE.Migrations
{
    public partial class AddProfileInterests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Main's EF model already uses these fields, but its snapshot/migrations
            // did not describe them. Additive and tolerant of manually seeded DBs.
            foreach (var table in new[] { "Collections", "Comments", "Conversations", "Messages", "Posts", "Reposts", "Stories" })
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ADD COLUMN IF NOT EXISTS \"Version\" integer NOT NULL DEFAULT 1;");
            foreach (var table in new[] { "Conversations", "Stories" })
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone NOT NULL DEFAULT NOW();");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Posts_Active_CreatedAt\" ON \"Posts\" (\"DeletedDayExpiredAt\", \"IsArchived\", \"CreatedAt\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Posts_Profile_Active_CreatedAt\" ON \"Posts\" (\"ProfileId\", \"DeletedDayExpiredAt\", \"IsArchived\", \"CreatedAt\");");

            migrationBuilder.CreateTable(
                name: "ProfileInterests",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topics = table.Column<string[]>(type: "text[]", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileInterests", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_ProfileInterests_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProfileInterests");
            // Keep legacy compatibility columns/indexes: main's model already needs
            // them and they may have existed before this migration.
        }
    }
}
