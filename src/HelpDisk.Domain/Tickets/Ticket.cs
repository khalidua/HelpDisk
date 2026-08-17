using HelpDisk.Domain.Primitives;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets.Events;

namespace HelpDisk.Domain.Tickets;

/// <summary>
/// A support request. The aggregate root of this template's main example.
/// </summary>
/// <remarks>
/// ============================================================================
/// READ THIS FILE FIRST. It is the point of the whole solution.
/// ============================================================================
///
/// Everything that is TRUE about a ticket, always, is enforced here - not in
/// TicketService, not in a validator, not in a database constraint. If you want
/// to know the rules of this business, you read this file, and you are done.
///
/// Three techniques make that possible:
///
///   1. PRIVATE SETTERS. You cannot write ticket.Status = TicketStatus.Closed.
///      The only route to a status change is Close(), which checks first.
///
///   2. A PRIVATE CONSTRUCTOR AND A STATIC FACTORY. new Ticket(...) does not
///      compile from outside. Create() is the only door, and it validates
///      before the object exists. There is no such thing as an invalid Ticket -
///      not "we check it later", but genuinely unrepresentable.
///
///   3. BEHAVIOUR METHODS THAT RETURN Result. Assign() refuses to work on a
///      closed ticket and says why. The refusal is a value, not an exception.
///
/// COMPARE with the anemic style you will meet in most codebases (and in the
/// MOJ reference), where Ticket is public properties only and the service does:
///
///     ticket.Status = TicketStatus.InProgress;   // did anyone check it wasn't closed?
///     ticket.AssigneeId = request.AssigneeId;    // who else writes to this field?
///
/// That compiles and it works, right up until a second service - a bulk import,
/// an admin screen, a background job - forgets one of the checks. The rule was
/// never in one place, so it was never really a rule.
///
/// THE PAYOFF, and the reason it is worth the extra typing: this class has no
/// dependencies. No DbContext, no repository, no HttpContext, no clock. To test
/// every rule below you write:
///
///     var ticket = Ticket.Create("Printer jammed", "3rd floor", TicketPriority.High,
///                                categoryId, "user-1").Value;
///     ticket.Close();
///     var result = ticket.Assign("agent-7");
///     Assert.Equal(TicketErrors.CannotAssignClosedTicket, result.Error);
///
/// No mocks. No in-memory database. No test fixture. Microseconds to run. That
/// is what "the Domain layer has no dependencies" actually buys you - and it is
/// why this template has no test project: the interesting tests need nothing
/// but the class itself.
/// </remarks>
public sealed class Ticket : AggregateRoot<Guid>, ISoftDeleteEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4_000;

    /// <summary>
    /// Backing field for <see cref="Comments"/>.
    /// </summary>
    /// <remarks>
    /// The collection is private and exposed read-only, so no caller can do
    /// ticket.Comments.Add(...) and skip the rules in AddComment.
    ///
    /// This has a cost, and it is the main practical friction of rich
    /// aggregates: EF Core cannot see a private field unless you tell it to.
    /// TicketConfiguration sets PropertyAccessMode.Field for exactly this
    /// reason. Skip that line and comments silently never load - EF reports no
    /// error, the list is simply always empty.
    /// </remarks>
    private readonly List<TicketComment> _comments = [];

    /// <summary>Required by EF Core. Not usable by application code.</summary>
    private Ticket()
    {
    }

    private Ticket(
        Guid id,
        string title,
        string description,
        TicketPriority priority,
        Guid categoryId,
        string reporterId)
        : base(id)
    {
        Title = title;
        Description = description;
        Priority = priority;
        CategoryId = categoryId;
        ReporterId = reporterId;
        Status = TicketStatus.New;
    }
    
    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public TicketStatus Status { get; private set; }

    public TicketPriority Priority { get; private set; }

    /// <summary>
    /// The category this ticket belongs to, referenced BY ID.
    /// </summary>
    /// <remarks>
    /// There is deliberately no <c>public Category Category { get; }</c>
    /// navigation property, even though EF Core would give you one for free and
    /// it would make some queries shorter.
    ///
    /// The DDD rule is that aggregates reference other aggregates by identity.
    /// A navigation property invites ticket.Category.Name = "..." - editing a
    /// second aggregate through the first, in one transaction, with none of
    /// Category's own rules consulted. Once that exists in a codebase, the
    /// boundaries stop meaning anything and you get the "load one entity, drag
    /// half the database into memory" problem.
    ///
    /// The cost is honest: showing a category name next to a ticket now needs a
    /// join or a second lookup. TicketService.SearchAsync does exactly that.
    /// </remarks>
    public Guid CategoryId { get; private set; }

    /// <summary>Who raised the ticket.</summary>
    public string ReporterId { get; private set; } = null!;

    /// <summary>Who is working on it, if anyone.</summary>
    public string? AssigneeId { get; private set; }

    public DateTime? ClosedOnUtc { get; private set; }

    // ISoftDeleteEntity. Public setters because SoftDeleteInterceptor writes
    // them; nothing in Domain or Application should.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime? RestoredAtUtc { get; set; }

    /// <summary>
    /// The ticket's comments, readable but not modifiable from outside.
    /// </summary>
    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();

    /// <summary>
    /// The only way to create a ticket.
    /// </summary>
    /// <remarks>
    /// Note the return type. This is not a constructor precisely BECAUSE it can
    /// fail: a constructor's only way to refuse is to throw, and we would
    /// rather return a described failure. The trade is that creation is now two
    /// steps - check the Result, then use .Value.
    ///
    /// The checks here overlap with CreateTicketRequestValidator, and that
    /// duplication is intentional, not an oversight. The validator gives the
    /// API a fast, friendly, field-by-field 400. This factory guarantees the
    /// rule holds for EVERY caller - a seeder, an import job, a future gRPC
    /// endpoint, a unit test - including the ones that will never pass through
    /// FluentValidation. Outer layers may check early; the domain checks
    /// always.
    /// </remarks>
    public static Result<Ticket> Create(
        string title,
        string description,
        TicketPriority priority,
        Guid categoryId,
        string reporterId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TicketErrors.TitleRequired;
        }

        if (title.Length > TitleMaxLength)
        {
            return TicketErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return TicketErrors.DescriptionRequired;
        }

        if (description.Length > DescriptionMaxLength)
        {
            return TicketErrors.DescriptionTooLong;
        }

        if (categoryId == Guid.Empty)
        {
            return TicketErrors.CategoryRequired;
        }

        if (string.IsNullOrWhiteSpace(reporterId))
        {
            return TicketErrors.ReporterRequired;
        }

        var ticket = new Ticket(
            Guid.NewGuid(),
            title.Trim(),
            description.Trim(),
            priority,
            categoryId,
            reporterId);

        // The Id already exists here, before any database round trip. That is
        // why Guid keys were chosen - see Entity<TKey>.
        ticket.RaiseDomainEvent(new TicketCreatedDomainEvent(ticket.Id, reporterId));

        return ticket;
    }

    /// <summary>
    /// Assigns the ticket to an agent and moves it to <see cref="TicketStatus.InProgress"/>.
    /// </summary>
    public Result Assign(string assigneeId)
    {
        if (string.IsNullOrWhiteSpace(assigneeId))
        {
            return TicketErrors.AssigneeRequired;
        }

        // The invariant. It lives here, so it holds for every caller forever.
        if (Status == TicketStatus.Closed)
        {
            return TicketErrors.CannotAssignClosedTicket;
        }

        AssigneeId = assigneeId;
        Status = TicketStatus.InProgress;

        RaiseDomainEvent(new TicketAssignedDomainEvent(Id, assigneeId));

        return Result.Success();
    }

    public Result Close()
    {
        if (Status == TicketStatus.Closed)
        {
            return TicketErrors.AlreadyClosed;
        }

        Status = TicketStatus.Closed;

        // A rare case where the domain needs the current time. This template
        // uses DateTime.UtcNow directly to keep Create() and Close() callable
        // with no arguments beyond their business inputs.
        //
        // The purist alternative is to pass IDateTimeProvider in, so tests can
        // freeze the clock. That is a real technique and IDateTimeProvider does
        // exist in this solution (Application/Abstractions) for the layers that
        // need it. It is not used here because it would put a service parameter
        // on every domain method for one timestamp - and the moment a domain
        // method takes services, people start passing repositories too.
        ClosedOnUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Reopen()
    {
        if (Status != TicketStatus.Closed)
        {
            return TicketErrors.NotClosed;
        }

        if(ClosedOnUtc is null || DateTime.UtcNow - ClosedOnUtc.Value > TimeSpan.FromDays(14))
        {
            return TicketErrors.CannotReopenExpiredTicket;
        }

        // Reopening returns the ticket to whoever had it, or to the unassigned
        // queue if nobody did. Encoding that here means every caller reopens a
        // ticket the same way.
        Status = AssigneeId is null ? TicketStatus.New : TicketStatus.InProgress;
        ClosedOnUtc = null;

        return Result.Success();
    }

    public Result UpdateDetails(string title, string description, TicketPriority priority)
    {
        if (Status == TicketStatus.Closed)
        {
            return TicketErrors.CannotEditClosedTicket;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return TicketErrors.TitleRequired;
        }

        if (title.Length > TitleMaxLength)
        {
            return TicketErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return TicketErrors.DescriptionRequired;
        }

        if (description.Length > DescriptionMaxLength)
        {
            return TicketErrors.DescriptionTooLong;
        }

        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;

        return Result.Success();
    }

    /// <summary>
    /// Adds a comment. The only way a <see cref="TicketComment"/> comes into
    /// existence.
    /// </summary>
    /// <remarks>
    /// This method is the aggregate boundary made concrete. The rule "you
    /// cannot comment on a closed ticket" is a rule ABOUT A TICKET, so it is
    /// enforced by the ticket - not by a comment service that would have to
    /// remember to load the parent and check its status.
    /// </remarks>
    public Result<TicketComment> AddComment(string body, string authorId)
    {
        if (Status == TicketStatus.Closed)
        {
            return TicketErrors.CannotCommentOnClosedTicket;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return TicketErrors.CommentBodyRequired;
        }

        if (body.Length > TicketComment.BodyMaxLength)
        {
            return TicketErrors.CommentBodyTooLong;
        }

        var comment = new TicketComment(Guid.NewGuid(), Id, body.Trim(), authorId);
        _comments.Add(comment);

        return comment;
    }
}
