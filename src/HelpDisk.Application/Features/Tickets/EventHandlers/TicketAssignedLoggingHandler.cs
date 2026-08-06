using HelpDisk.Application.Abstractions.Events;
using HelpDisk.Domain.Tickets.Events;
using Microsoft.Extensions.Logging;

namespace HelpDisk.Application.Features.Tickets.EventHandlers;

/// <summary>
/// Reacts to a ticket being assigned.
/// </summary>
/// <remarks>
/// In a real system this would send the assignee an e-mail or a Slack message.
/// It logs instead, because a template that needs an SMTP server to demonstrate
/// domain events is a template nobody runs.
///
/// The shape is what matters. To add a real notification you would:
///
///   1. Declare IEmailService in Application/Abstractions.
///   2. Implement it in Infrastructure/Services.
///   3. Inject it here and replace the log line.
///
/// Notice what does NOT change: not Ticket, not TicketService, not
/// TicketsController. The business operation "assign a ticket" is complete and
/// correct without knowing that anybody gets notified - which is precisely the
/// coupling domain events exist to remove.
///
/// To add a SECOND reaction - say, a metrics counter - you write another class
/// implementing IDomainEventHandler&lt;TicketAssignedDomainEvent&gt; and register
/// it. You edit nothing that already works. That is the payoff.
/// </remarks>
public sealed class TicketAssignedLoggingHandler : IDomainEventHandler<TicketAssignedDomainEvent>
{
    private readonly ILogger<TicketAssignedLoggingHandler> _logger;

    public TicketAssignedLoggingHandler(ILogger<TicketAssignedLoggingHandler> logger) =>
        _logger = logger;

    public Task HandleAsync(
        TicketAssignedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ticket {TicketId} was assigned to {AssigneeId}. (A real system would notify them here.)",
            domainEvent.TicketId,
            domainEvent.AssigneeId);

        // Nothing async to await. Returning Task.CompletedTask keeps the
        // interface uniform for handlers that do need to await something.
        return Task.CompletedTask;
    }
}
