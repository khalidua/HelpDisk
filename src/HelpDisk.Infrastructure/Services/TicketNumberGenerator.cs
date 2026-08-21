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
        var number = await _context.Database
            .SqlQueryRaw<long>(
                "SELECT NEXT VALUE FOR TicketNumberSequence")
            .SingleAsync(cancellationToken);

        return $"TKT-{_dateTimeProvider.UtcNow.Year}-{number:D5}";
    }
}