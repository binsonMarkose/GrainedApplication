using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grained.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrowthSeasonEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsOnUtc",
                table: "GrowthSeasons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Backfill: each existing season ends where the next one (same church) begins…
            migrationBuilder.Sql(@"
                UPDATE ""GrowthSeasons"" s
                SET ""EndsOnUtc"" = nxt.next_start
                FROM (
                    SELECT ""Id"", LEAD(""StartsOnUtc"") OVER (PARTITION BY ""ChurchId"" ORDER BY ""StartsOnUtc"") AS next_start
                    FROM ""GrowthSeasons""
                ) nxt
                WHERE s.""Id"" = nxt.""Id"" AND nxt.next_start IS NOT NULL;");

            // …and the latest season of each church (no next) gets a 52-week default year.
            migrationBuilder.Sql(@"
                UPDATE ""GrowthSeasons""
                SET ""EndsOnUtc"" = ""StartsOnUtc"" + INTERVAL '1 year'
                WHERE ""EndsOnUtc"" <= ""StartsOnUtc"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndsOnUtc",
                table: "GrowthSeasons");
        }
    }
}
