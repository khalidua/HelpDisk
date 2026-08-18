using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResponseDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDeadlineUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseDeadlineUtc",
                table: "Tickets");
        }
    }
}
