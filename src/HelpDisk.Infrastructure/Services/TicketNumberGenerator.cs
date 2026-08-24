using System.Data;

using HelpDisk.Application.Abstractions;
using HelpDisk.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HelpDisk.Infrastructure.Services;

public sealed class TicketNumberGenerator : ITicketNumberGenerator
{
    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TicketNumberGenerator(
        AppDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT NEXT VALUE FOR TicketNumberSequence";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        var number = Convert.ToInt64(result);

        return $"TKT-{_dateTimeProvider.UtcNow.Year}-{number:D5}";
    }
}