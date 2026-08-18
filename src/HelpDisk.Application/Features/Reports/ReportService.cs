using HelpDisk.Application.Features.Reports.Dtos;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;

namespace HelpDisk.Application.Features.Reports;

public sealed class ReportService : IReportService
{
    private readonly ITicketRepository _ticketRepository;
    public ReportService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result<IReadOnlyList<OpenTicketsPerAgentResponse>>> GetOpenTicketsPerAgentAsync(
        CancellationToken cancellationToken)
    {
        var openTicketsPerAgent = await _ticketRepository.GetOpenTicketsPerAgentAsync(cancellationToken);
        var responses = openTicketsPerAgent
            .Select(x => new OpenTicketsPerAgentResponse(
                x.AgentId,
                x.OpenTicketsCount))
            .ToList();
        return Result.Success<IReadOnlyList<OpenTicketsPerAgentResponse>>(responses);
    }

    public async Task<Result<IReadOnlyList<AverageResolutionTimePerCategoryResponse>>> GetAverageResolutionTimePerCategoryAsync(CancellationToken cancellationToken)
    {
        var averageResolutionTimePerCategory = await _ticketRepository.GetAverageResolutionTimePerCategoryAsync(cancellationToken);
        var responses = averageResolutionTimePerCategory
            .Select(x => new AverageResolutionTimePerCategoryResponse(
                x.CategoryId,
                x.AverageResolutionTimeInHours))
            .ToList();
        return Result.Success<IReadOnlyList<AverageResolutionTimePerCategoryResponse>>(responses);
    }

    public async Task<Result<SlaBreachesThisMonthResponse>> GetSlaBreachesThisMonthAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1);
        var nextMonthStartUtc = monthStartUtc.AddMonths(1);
        var slaBreaches = await _ticketRepository.GetSlaBreachesThisMonthAsync(monthStartUtc, nextMonthStartUtc, cancellationToken);
        var response = new SlaBreachesThisMonthResponse(slaBreaches.BreachCount);
        return Result.Success<SlaBreachesThisMonthResponse>(response);
    }
}
