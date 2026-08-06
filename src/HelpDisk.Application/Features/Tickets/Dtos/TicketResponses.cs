using HelpDisk.Domain.Tickets;

namespace HelpDisk.Application.Features.Tickets.Dtos;

/*
 * ============================================================================
 * RESPONSE DTOs - what goes OUT to the outside world.
 * ============================================================================
 *
 * Returning entities directly is the single most common leak in a layered
 * codebase, and it is tempting because at first the entity has exactly the
 * right fields. Then:
 *
 *   - Ticket gains an InternalCostEstimate field and it is now in your public
 *     JSON. Nobody decided that. It just happened.
 *   - The serialiser touches a lazy navigation property and fires a database
 *     query during response writing, outside your transaction.
 *   - A circular reference (Ticket -> Comments -> Ticket) throws at
 *     serialisation time, in production, not at compile time.
 *   - You cannot rename a domain property without breaking every client.
 *
 * A DTO is a deliberate, stable, public contract. It is worth the typing.
 *
 * Note the two shapes below. TicketListItemResponse is smaller than
 * TicketResponse - a list of 200 tickets has no business shipping 200 full
 * descriptions and every comment. Different views, different DTOs.
 */

/// <summary>Full detail for a single ticket, including its comments.</summary>
public sealed record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CategoryId,
    string ReporterId,
    string? AssigneeId,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc,
    DateTime? ClosedOnUtc,
    IReadOnlyList<TicketCommentResponse> Comments);

public sealed record TicketCommentResponse(
    Guid Id,
    string Body,
    string AuthorId,
    DateTime CreatedOnUtc);

/// <summary>
/// The trimmed shape used in search results. No Description, no Comments.
/// </summary>
public sealed record TicketListItemResponse(
    Guid Id,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CategoryId,
    string? AssigneeId,
    DateTime CreatedOnUtc);

/// <summary>
/// A page of results. Mirrors Domain's Pagination&lt;T&gt; but carries DTOs, so
/// the pager metadata reaches the client without the entities coming with it.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int CurrentPage,
    int PageSize,
    int TotalPages,
    int TotalItems,
    bool HasPreviousPage,
    bool HasNextPage);
