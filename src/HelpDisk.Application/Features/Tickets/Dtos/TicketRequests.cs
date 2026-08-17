using HelpDisk.Domain.Tickets;

namespace HelpDisk.Application.Features.Tickets.Dtos;

/*
 * ============================================================================
 * REQUEST DTOs - what comes IN from the outside world.
 * ============================================================================
 *
 * WHY NOT JUST ACCEPT A Ticket?
 *
 * Three reasons, and each has bitten real projects:
 *
 *   1. Ticket has no public constructor and no public setters, so a JSON
 *      deserialiser cannot build one. That is not an obstacle to work around -
 *      it is the aggregate refusing to be created without going through
 *      Ticket.Create. Good.
 *
 *   2. Binding straight to an entity is mass assignment. Add an internal-only
 *      field to Ticket - IsEscalated, InternalNotes, Cost - and every caller can
 *      now set it by adding one line of JSON. Nobody changed the API on
 *      purpose; it changed because the entity did.
 *
 *   3. Your API contract and your database schema change for different reasons
 *      and at different speeds. Tying them together means every schema tweak is
 *      a breaking API change.
 *
 * These are records because they are immutable input, and grouped in one file
 * because they are read together. Responses live next door in
 * TicketResponses.cs.
 */

/// <summary>
/// Note what is ABSENT: there is no ReporterId. The client does not get to say
/// who is reporting - TicketService takes that from ICurrentUser. Any field a
/// caller could lie about to their advantage does not belong in a request DTO.
/// </summary>
public sealed record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid CategoryId);

public sealed record UpdateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority);

public sealed record AssignTicketRequest(string AssigneeId);

/// <summary>
/// Likewise no AuthorId - that comes from ICurrentUser.
/// </summary>
public sealed record AddCommentRequest(string Body, bool IsInternal = false);

/// <summary>
/// Search filters. Every field is optional; supplying none returns page 1 of
/// everything.
/// </summary>
public sealed record TicketSearchRequest(
    string? Keyword = null,
    TicketStatus? Status = null,
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 20);
