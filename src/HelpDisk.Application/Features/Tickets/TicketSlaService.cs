using HelpDisk.Domain.Tickets;
using HelpDisk.Domain.Repositories;
using HelpDisk.Application.Abstractions;

namespace HelpDisk.Application.Features.Tickets;

public sealed class TicketSlaService
{
    private readonly ITicketRepository _tickets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTime;

    public TicketSlaService(
        ITicketRepository tickets,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTime)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _dateTime = dateTime;
    }

    public async Task CheckExpiredSlaAsync(
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTime.UtcNow;

        var tickets = await _tickets.GetExpiredSlaTicketsAsync(nowUtc, cancellationToken);
        var hasChanges = false;
        foreach (var ticket in tickets)
        {
            var result = ticket.MarkSlaBreached();
            ticket.MarkSlaBreached();

            if (result.IsFailure)
            {
                continue;
            }
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}