using HelpDisk.Domain.Shared;

namespace HelpDisk.Domain.Tickets;

/// <summary>
/// How the application reaches stored tickets.
/// </summary>
/// <remarks>
/// ============================================================================
/// THE DEPENDENCY INVERSION EXAMPLE. This interface is why Application can talk
/// to a database without referencing one.
/// ============================================================================
///
/// The interface is declared HERE, in Domain, next to the aggregate it serves.
/// The implementation (TicketRepository, full of EF Core) lives out in
/// Infrastructure. So the arrow points inward: Infrastructure depends on
/// Domain, never the reverse.
///
/// Read that again, because it is the bit that feels backwards at first. The
/// consumer owns the contract, not the provider. Domain says "I need something
/// that can fetch a ticket by id"; Infrastructure obeys.
///
/// ---------------------------------------------------------------------------
/// WHY NOT IGenericRepository<T>?
///
/// The MOJ reference codebase uses one generic repository for every entity:
///
///     IGenericRepository<Ticket>  with  GetQueryable(), GetWithSpec(),
///                                       GetByPropertyAsync(predicate), ...
///
/// It is less code, and you will meet it in the wild. The trade-offs:
///
///   FOR generic:      one implementation covers every future aggregate; a new
///                     entity needs no new repository at all.
///
///   AGAINST generic:  GetQueryable() hands an IQueryable to the Application
///                     layer, and now query construction - joins, includes,
///                     filters - lives in services. Two things follow. First,
///                     EF Core's translation rules leak upward: your service
///                     silently depends on what this provider can translate, so
///                     "swap the database" stops being real. Second, callers
///                     can compose queries the aggregate never sanctioned, such
///                     as loading a TicketComment without its Ticket - and the
///                     boundary quietly stops existing.
///
/// This template chooses one interface per aggregate, with methods named for
/// what the business wants rather than how it is fetched. The cost is a new
/// interface per aggregate. The gain is that this file is a readable, finite
/// list of every way tickets can be read - and no caller can invent a new one.
/// ---------------------------------------------------------------------------
///
/// Note there is no SaveAsync here. Repositories track changes; IUnitOfWork
/// commits them.
/// </remarks>
public interface ITicketRepository
{
    /// <summary>Loads a ticket on its own. Comments are NOT included.</summary>
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a ticket together with its comments.
    /// </summary>
    /// <remarks>
    /// A separate method rather than a bool flag or an Include parameter,
    /// because the choice matters: AddComment needs the collection loaded, but
    /// a status change does not, and pulling every comment for a two-year-old
    /// ticket to flip one enum is how endpoints get slow. Naming the two cases
    /// makes the cost visible at the call site.
    /// </remarks>
    Task<Ticket?> GetWithCommentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Searches tickets, newest first, one page at a time.</summary>
    Task<Pagination<Ticket>> SearchAsync(
        string? keyword,
        TicketStatus? status,
        TicketPriority? priority,
        Guid? categoryId,
        string? assigneeId,
        DateTime? fromDate,
        DateTime? toDate,
        string? reporterId,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins tracking a new ticket. Nothing is written until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a ticket for deletion. Synchronous and void-returning precisely
    /// because it does not touch the database - it only changes EF's opinion of
    /// the entity's state.
    /// </summary>
    /// <remarks>
    /// This will not actually delete anything: SoftDeleteInterceptor converts
    /// it to an update setting IsDeleted = true. The caller does not know that,
    /// and does not need to.
    /// </remarks>
    void Remove(Ticket ticket);
}
