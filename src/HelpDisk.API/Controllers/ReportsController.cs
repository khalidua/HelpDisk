using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Reports;
using HelpDisk.Application.Features.Tickets;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("opened-tickets-per-agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetOpenedTicketsPerAgent(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetOpenTicketsPerAgentAsync(cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("average-resolution-time-per-category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAverageResolutionTimePerCategory(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAverageResolutionTimePerCategoryAsync(cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("sla-breaches-this-month")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetSlaBreachesThisMonth(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSlaBreachesThisMonthAsync(cancellationToken);
        return HandleResult(result);
    }
}
