using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiftAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFreemiumChangesToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatDailyCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChatDailyLastResetUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatDailyCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ChatDailyLastResetUtc",
                table: "AspNetUsers");
        }
    }
}
