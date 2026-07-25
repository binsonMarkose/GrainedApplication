using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grained.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "LessonClassGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Seed a stable teaching order for existing assignments: within each group, order by the
            // lesson's creation date (0-based). Admins can then drag to rearrange.
            migrationBuilder.Sql(@"
                UPDATE ""LessonClassGroups"" lcg
                SET ""SortOrder"" = ordered.rn
                FROM (
                    SELECT lcg2.""Id"",
                           ROW_NUMBER() OVER (PARTITION BY lcg2.""ClassGroupId"" ORDER BY l.""CreatedAtUtc"", l.""Title"") - 1 AS rn
                    FROM ""LessonClassGroups"" lcg2
                    JOIN ""Lessons"" l ON l.""Id"" = lcg2.""LessonId""
                ) ordered
                WHERE ordered.""Id"" = lcg.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "LessonClassGroups");
        }
    }
}
