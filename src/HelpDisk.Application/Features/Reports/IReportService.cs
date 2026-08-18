using HelpDisk.Application.Features.Reports.Dtos;
using HelpDisk.Domain.Shared;

public interface IReportService
{
    Task<Result<IReadOnlyList<OpenTicketsPerAgentResponse>>> GetOpenTicketsPerAgentAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AverageResolutionTimePerCategoryResponse>>> GetAverageResolutionTimePerCategoryAsync(CancellationToken cancellationToken);

    Task<Result<SlaBreachesThisMonthResponse>> GetSlaBreachesThisMonthAsync(CancellationToken cancellationToken);
}
