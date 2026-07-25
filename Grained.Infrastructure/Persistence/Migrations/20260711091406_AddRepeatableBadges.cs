using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grained.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRepeatableBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChildBadges_ChildId_BadgeId",
                table: "ChildBadges");

            migrationBuilder.AddColumn<bool>(
                name: "Repeatable",
                table: "Badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing effort/character (Standard = tier 0) badges become repeatable; milestones
            // (Achievement = tier 1) stay one-time.
            migrationBuilder.Sql("UPDATE \"Badges\" SET \"Repeatable\" = true WHERE \"Tier\" = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_ChildId_BadgeId",
                table: "ChildBadges",
                columns: new[] { "ChildId", "BadgeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChildBadges_ChildId_BadgeId",
                table: "ChildBadges");

            migrationBuilder.DropColumn(
                name: "Repeatable",
                table: "Badges");

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_ChildId_BadgeId",
                table: "ChildBadges",
                columns: new[] { "ChildId", "BadgeId" },
                unique: true);
        }
    }
}
