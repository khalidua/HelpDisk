using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class isInternalInTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "TicketComments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "TicketComments");
        }
    }
}
