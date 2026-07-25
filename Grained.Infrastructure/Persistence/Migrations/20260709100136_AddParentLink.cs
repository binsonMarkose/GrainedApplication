using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grained.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentUserId",
                table: "Children",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Children_ParentUserId",
                table: "Children",
                column: "ParentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Children_AspNetUsers_ParentUserId",
                table: "Children",
                column: "ParentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Children_AspNetUsers_ParentUserId",
                table: "Children");

            migrationBuilder.DropIndex(
                name: "IX_Children_ParentUserId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ParentUserId",
                table: "Children");
        }
    }
}
