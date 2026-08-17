using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Tickets;
using HelpDisk.Application.Features.Tickets.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HelpDisk.API.Controllers;

/// <summary>
/// HTTP endpoints for the Ticket feature.
/// </summary>
/// <remarks>
/// ============================================================================
/// NOTICE HOW LITTLE THIS FILE DOES.
/// ============================================================================
///
/// Every action is three lines: take the request, call the service, hand the
/// Result to the base class. There is no business logic here, no validation, no
/// mapping, no database access - and there never should be.
///
/// A controller's entire job is TRANSLATION between HTTP and the application:
///   - route and body -> a request DTO
///   - a Result       -> a status code and a JSON body
///
/// Its one dependency is ITicketService, an Application interface. It cannot
/// see EF Core, Ticket, or ITicketRepository - the API project references
/// Infrastructure only so Program.cs can wire it up, and controllers stay on
/// the Application side of that line.
///
/// THE TEST TO APPLY when reviewing a controller: if you deleted it and wrote a
/// gRPC service, a background worker or a CLI over the same ITicketService,
/// would any business behaviour be lost? If yes, logic has leaked upward into
/// a layer that should only be translating.
///
/// (For comparison, the MOJ reference injects MediatR's ISender and sends a
/// command instead of calling a service. Same thinness, one more hop of
/// indirection.)
/// </remarks>

[Authorize]
[Route("api/tickets")]
public sealed class TicketsController : ApiController
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService) => _ticketService = ticketService;

    /// <summary>Creates a ticket.</summary>
    /// <remarks>
    /// Returns the new id. The reporter is NOT taken from the body - it comes
    /// from ICurrentUser inside the service, so a caller cannot raise a ticket
    /// in somebody else's name.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.CreateAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gets one ticket, including its comments.</summary>
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetByIdAsync(ticketId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Searches tickets with optional filters and paging.</summary>
    /// <remarks>
    /// [FromQuery] on a record binds each property from the query string, so
    /// GET /api/tickets?status=New&amp;page=2 works with no extra plumbing.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TicketListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.SearchAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Updates a ticket's title, description and priority.</summary>
    /// <remarks>Returns 409 if the ticket is closed.</remarks>
    [HttpPut("{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid ticketId,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.UpdateAsync(ticketId, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Assigns a ticket to an agent.</summary>
    /// <remarks>
    /// A separate endpoint rather than a field on the update payload, because
    /// assignment is a distinct business action with its own rules (a closed
    /// ticket cannot be assigned) and its own domain event. Routes that mirror
    /// what the business DOES age better than routes that mirror table columns.
    /// </remarks>
    [HttpPut("{ticketId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(
        Guid ticketId,
        [FromBody] AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.AssignAsync(ticketId, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Closes a ticket.</summary>
    [HttpPut("{ticketId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketService.CloseAsync(ticketId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Reopens a closed ticket.</summary>
    [HttpPut("{ticketId:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketService.ReopenAsync(ticketId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Adds a comment to a ticket.</summary>
    /// <remarks>
    /// Nested under the ticket because a comment has no independent existence -
    /// the URL mirrors the aggregate boundary. There is deliberately no
    /// /api/comments/{id} endpoint.
    /// </remarks>
    [HttpPost("{ticketId:guid}/comments")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddComment(
        Guid ticketId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.AddCommentAsync(ticketId, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Deletes a ticket (soft delete - the row survives).</summary>
    [HttpDelete("{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketService.DeleteAsync(ticketId, cancellationToken);
        return HandleResult(result);
    }
}
