using HelpDisk.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace HelpDisk.Infrastructure.Persistence;

/// <summary>
/// The EF Core implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// Barely more than a wrapper over DbContext - which is the point. EF Core's
/// DbContext ALREADY is a unit of work: it tracks changes and commits them
/// together. This class exists so Application can say "commit now" while
/// referencing nothing from Microsoft.EntityFrameworkCore.
///
/// A fair question from students: is a one-line pass-through worth a class?
/// Here, yes - it is what stops the EF namespace appearing in every service.
/// But it is a genuine judgement call, and a team that has decided it will
/// never move off EF Core could reasonably inject DbContext directly and save
/// the ceremony. Know why you are paying, and do not pretend the cost is zero.
///
/// ---------------------------------------------------------------------------
/// THE TRANSACTION FIELD IS THE INTERESTING PART.
///
/// _transaction is IDbContextTransaction - an EF Core type - and it is PRIVATE.
/// The interface returns plain Task. So the capability crosses the boundary
/// while the type does not.
///
/// The MOJ reference instead declares
/// Task&lt;IDbContextTransaction&gt; BeginTransactionAsync() on its Domain
/// interface, which forces Domain to reference EF Core and quietly ends the
/// layer's independence. Same feature, one word of difference in the signature,
/// completely different architectural consequence. This is the kind of detail
/// that decides whether the diagram on the wall matches the code.
/// ---------------------------------------------------------------------------
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context) => _context = context;

    /// <summary>
    /// Writes every tracked change in one database transaction.
    /// </summary>
    /// <remarks>
    /// EF wraps SaveChanges in its own transaction automatically, so the
    /// explicit Begin/Commit methods below are only needed when ONE business
    /// operation must span several saves. Nothing in this template needs that;
    /// they are here because a real application eventually will, and because
    /// the field they use is the teaching point above.
    ///
    /// This call is also what triggers all three interceptors: audit stamps and
    /// soft-delete conversion before the SQL, domain event dispatch after it.
    /// </remarks>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no transaction to commit.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // Dispose in a finally so a failed commit still releases the
            // connection. Without this, one exception leaks a transaction and
            // the next request blocks on a lock that is never released.
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
