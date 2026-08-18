using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSlaStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlaStatus",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaStatus",
                table: "Tickets");
        }
    }
}
