using HelpDisk.Domain.Primitives;

namespace HelpDisk.Domain.Tickets;

/// <summary>
/// A note added to a ticket. Part of the Ticket aggregate, never independent.
/// </summary>
/// <remarks>
/// Notice what is NOT here:
///
///   - No public constructor. The only way to make one is Ticket.AddComment,
///     which is what guarantees a comment can never exist without a ticket, and
///     can never be added to a closed ticket.
///   - No ITicketCommentRepository. Comments are loaded through their ticket
///     (ITicketRepository.GetWithCommentsAsync) and saved when their ticket is
///     saved. Adding a comment repository would let callers bypass Ticket's
///     rules entirely - which is exactly the hole aggregates exist to close.
///   - No Result-returning methods. Validation happened in Ticket.AddComment
///     before this object existed. An entity inside an aggregate can trust that
///     its root already checked.
///
/// If a comment ever needed to be edited or deleted independently, that would
/// be a signal it has become its own aggregate - and this is the file where you
/// would notice.
/// </remarks>
public sealed class TicketComment : Entity<Guid>
{
    public const int BodyMaxLength = 2_000;

    /// <summary>Required by EF Core.</summary>
    private TicketComment()
    {
    }

    /// <summary>
    /// Internal so that only the Domain assembly - in practice, only
    /// <see cref="Ticket.AddComment"/> - can create one.
    /// </summary>
    internal TicketComment(Guid id, Guid ticketId, string body, string authorId, bool isInternal)
        : base(id)
    {
        TicketId = ticketId;
        Body = body;
        AuthorId = authorId;
        IsInternal = isInternal;
    }

    public Guid TicketId { get; private set; }

    public string Body { get; private set; } = null!;

    public string AuthorId { get; private set; } = null!;
    public bool IsInternal { get; private set; }
}
