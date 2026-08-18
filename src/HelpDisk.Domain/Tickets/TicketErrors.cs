using HelpDisk.Domain.Shared;

namespace HelpDisk.Domain.Tickets;

/// <summary>
/// Every failure the Ticket aggregate can produce, named once.
/// </summary>
/// <remarks>
/// Collecting errors per aggregate is a small habit with a large payoff.
///
///   - The full list of ways this feature can fail is readable in one screen.
///     Try answering "what can go wrong when assigning a ticket?" in a codebase
///     that builds error strings inline.
///   - Codes are defined once, so the front-end and translation files key off
///     something stable. Inline strings drift: "Ticket.NotFound" in one handler
///     and "TicketNotFound" in another, and now the client needs both.
///   - Renaming a code is a compile-time change, not a grep-and-pray.
///
/// Codes follow "Aggregate.Reason" so the origin is obvious in a log line.
/// </remarks>
public static class TicketErrors
{
    public static Error NotFound(Guid ticketId) => Error.NotFound(
        "Ticket.NotFound",
        $"No ticket was found with id '{ticketId}'.");

    public static readonly Error TitleRequired = Error.Validation(
        "Ticket.TitleRequired",
        "A ticket must have a title.");

    public static readonly Error TitleTooLong = Error.Validation(
        "Ticket.TitleTooLong",
        $"A ticket title cannot exceed {Ticket.TitleMaxLength} characters.");

    public static readonly Error DescriptionRequired = Error.Validation(
        "Ticket.DescriptionRequired",
        "A ticket must have a description.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "Ticket.DescriptionTooLong",
        $"A ticket description cannot exceed {Ticket.DescriptionMaxLength} characters.");

    public static readonly Error ReporterRequired = Error.Validation(
        "Ticket.ReporterRequired",
        "A ticket must have a reporter.");

    public static readonly Error CategoryRequired = Error.Validation(
        "Ticket.CategoryRequired",
        "A ticket must belong to a category.");

    public static readonly Error AssigneeRequired = Error.Validation(
        "Ticket.AssigneeRequired",
        "An assignee must be supplied.");

    public static readonly Error InvalidAssignee = Error.Validation(
    "Ticket.InvalidAssignee",
    "A ticket can only be assigned to an agent or admin.");

    public static readonly Error CommentBodyRequired = Error.Validation(
        "Ticket.CommentBodyRequired",
        "A comment cannot be empty.");

    public static readonly Error CommentBodyTooLong = Error.Validation(
        "Ticket.CommentBodyTooLong",
        $"A comment cannot exceed {TicketComment.BodyMaxLength} characters.");

    // ---- State-transition failures -----------------------------------------
    // These are Conflict rather than Validation, and the distinction is not
    // pedantic: the request was perfectly well-formed, it just arrived when the
    // ticket was in a state that forbids it. ApiController turns Conflict into
    // 409 and Validation into 400 - which tells the caller whether fixing the
    // payload would help.

    public static readonly Error CannotAssignClosedTicket = Error.Conflict(
        "Ticket.CannotAssignClosed",
        "A closed ticket cannot be assigned. Reopen it first.");

    public static readonly Error CannotCommentOnClosedTicket = Error.Conflict(
        "Ticket.CannotCommentOnClosed",
        "A closed ticket cannot receive new comments. Reopen it first.");

    public static readonly Error AlreadyClosed = Error.Conflict(
        "Ticket.AlreadyClosed",
        "This ticket is already closed.");

    public static readonly Error NotClosed = Error.Conflict(
        "Ticket.NotClosed",
        "Only a closed ticket can be reopened.");

    public static readonly Error CannotReopenExpiredTicket = Error.Conflict(
    "Ticket.CannotReopenExpired",
    "A ticket can only be reopened within 14 days of being closed.");

    public static readonly Error CannotEditClosedTicket = Error.Conflict(
        "Ticket.CannotEditClosed",
        "A closed ticket cannot be edited. Reopen it first.");

    public static readonly Error InternalCommentNotAllowed = Error.Validation(
    "Ticket.InternalCommentNotAllowed",
    "Customers cannot create internal comments.");

    public static readonly Error InvalidResponseDeadline = Error.Validation(
    "Ticket.InvalidResponseDeadline",
    "The response deadline must be after the ticket creation time.");

    public static readonly Error SlaAlreadyResolved = Error.Validation(
    "Ticket.SlaAlreadyResolved",
    "The ticket SLA has already been resolved.");
}
