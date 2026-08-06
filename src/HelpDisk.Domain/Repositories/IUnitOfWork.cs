namespace HelpDisk.Domain.Repositories;

/// <summary>
/// Commits everything a single business operation changed, as one transaction.
/// </summary>
/// <remarks>
/// WHY SEPARATE FROM THE REPOSITORY?
///
/// Look at TicketService.AddCommentAsync: it loads a ticket, adds a comment,
/// and may flip the ticket's status. That is one business operation touching
/// several rows. If each repository call saved immediately, a failure halfway
/// through would leave the database in a state your domain says is impossible.
///
/// So repositories only TRACK changes. IUnitOfWork decides when they are
/// written. Repository methods here are deliberately synchronous and
/// void-returning (see ITicketRepository.Remove) to reinforce that they do not
/// touch the database.
///
/// EF Core's DbContext already is a unit of work - this interface exists so
/// Application can say "commit now" without referencing EF Core.
///
/// ---------------------------------------------------------------------------
/// A LEAK WORTH POINTING AT
///
/// The MOJ reference codebase declares this instead:
///
///     Task&lt;IDbContextTransaction&gt; BeginTransactionAsync(...);
///
/// IDbContextTransaction is an EF Core type. Returning it from a Domain
/// interface drags Microsoft.EntityFrameworkCore.Relational into Domain and
/// quietly destroys the independence the layer exists to provide.
///
/// Here the transaction methods return plain Task. Infrastructure holds the
/// EF transaction object privately. Same capability, no leak. This is the kind
/// of detail that decides whether Clean Architecture is real in a codebase or
/// just a folder layout.
/// ---------------------------------------------------------------------------
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Writes all tracked changes. Also the moment domain events are dispatched
    /// - see DomainEventsInterceptor.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
