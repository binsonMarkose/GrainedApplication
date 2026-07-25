using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grained.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAuthoringWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_ChurchId",
                table: "Lessons");

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Lessons",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorUserId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginChurchId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Lessons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceLessonId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: existing published lessons become Status = Published (2); drafts stay 0.
            migrationBuilder.Sql("UPDATE \"Lessons\" SET \"Status\" = 2 WHERE \"IsPublished\" = true;");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_AuthorUserId",
                table: "Lessons",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ChurchId_Status",
                table: "Lessons",
                columns: new[] { "ChurchId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_AuthorUserId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_ChurchId_Status",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "AuthorUserId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "OriginChurchId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SourceLessonId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ChurchId",
                table: "Lessons",
                column: "ChurchId");
        }
    }
}
