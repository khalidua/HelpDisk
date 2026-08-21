using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateSequence(
                name: "TicketNumberSequence",
                startValue: 1L);

            migrationBuilder.Sql("""
        UPDATE Tickets
        SET TicketNumber =
            'TKT-' +
            CAST(YEAR(CreatedOnUtc) AS varchar(4)) +
            '-' +
            RIGHT('00000' + CAST(NEXT VALUE FOR TicketNumberSequence AS varchar(5)), 5)
        WHERE TicketNumber IS NULL;
        """);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "Tickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketNumber",
                table: "Tickets",
                column: "TicketNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                table: "Tickets");

            migrationBuilder.DropSequence(
                name: "TicketNumberSequence");
        }
    }
}
