using HelpDisk.Domain.Reports;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.Infrastructure.Persistence.Repositories;

/// <summary>
/// The EF Core implementation of <see cref="ITicketRepository"/>.
/// </summary>
/// <remarks>
/// The other half of the dependency inversion. The interface is declared in
/// Domain, next to Ticket; the EF Core code that satisfies it is here, in
/// Infrastructure. Application binds to the interface and never learns which
/// one it got.
///
/// Everything database-shaped is confined to this file: Include, AsNoTracking,
/// query translation, Skip/Take. Moving to Dapper or Postgres means rewriting
/// this class and touching nothing above it. That claim is easy to make and
/// hard to keep - the way it is kept is by never letting an IQueryable escape,
/// which is exactly what a generic GetQueryable() repository gives away.
/// </remarks>
public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context) => _context = context;

    /// <summary>
    /// Loads a ticket for modification. TRACKED, deliberately.
    /// </summary>
    /// <remarks>
    /// No AsNoTracking here. Callers load a ticket in order to change it, and
    /// EF must be watching in order to notice - that is what makes
    /// IUnitOfWork.SaveChangesAsync work with no explicit Update call.
    ///
    /// Add AsNoTracking to this method and every write in the application stops
    /// persisting. Silently. Read methods are the ones that want it - see
    /// SearchAsync.
    /// </remarks>
    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Ticket?> GetWithCommentsAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Ticket?> GetWithAttachmentsAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Ticket?> GetWithCommentsAndAttachmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);
    }
    /// <summary>
    /// Paged, filtered search. NOT tracked.
    /// </summary>
    /// <remarks>
    /// AsNoTracking because nothing here will be modified. It skips building
    /// the change-tracking graph, which on a list endpoint is measurable - and
    /// it removes any chance of a read accidentally writing.
    ///
    /// Note the two round trips: one CountAsync for the total, one ToListAsync
    /// for the page. Both are needed - the client cannot render a pager without
    /// knowing how many there are.
    ///
    /// A REAL LIMITATION, stated honestly: Skip/Take paging degrades on large
    /// tables, because the database must still walk the rows it skips. Page
    /// 10,000 is slow no matter how good the index. Keyset pagination ("give me
    /// the 20 after this timestamp") is the answer at scale, and it is more
    /// complex than this template needs.
    /// </remarks>
    public async Task<Pagination<Ticket>> SearchAsync(
        string? keyword,
        TicketStatus? status,
        TicketPriority? priority,
        Guid? categoryId,
        string? assigneeId,
        DateTime? fromDate,
        DateTime? toDate,
        string? reporterId,
        Guid? companyId,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // The global query filter already excludes soft-deleted rows, so there
        // is no "&& !t.IsDeleted" here. It is applied by EF, everywhere.
        var query = _context.Tickets.AsNoTracking();

        // Filters are composed conditionally. Nothing executes yet - IQueryable
        // is a description of a query, and the SQL is not built until an
        // awaited terminal operation below.
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var trimmed = keyword.Trim();
            query = query.Where(t =>
                t.Title.Contains(trimmed) ||
                t.Description.Contains(trimmed));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(t => t.AssigneeId == assigneeId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedOnUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedOnUtc <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(reporterId))
        {
            query = query.Where(t => t.ReporterId == reporterId);
        }

        if (companyId.HasValue)
        {
            query = query.Where(t =>
                _context.Users.Any(u =>
                    u.Id == t.ReporterId &&
                    u.CompanyId == companyId.Value));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first. An unordered Skip/Take is undefined behaviour - the
            // database may return rows in any order, so page 2 could repeat a
            // row from page 1.
            .OrderByDescending(t => t.CreatedOnUtc)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new Pagination<Ticket>(page, pageSize, totalItems, items);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Tickets.AnyAsync(t => t.Id == id, cancellationToken);

    /// <summary>
    /// Starts tracking a new ticket. Writes nothing.
    /// </summary>
    /// <remarks>
    /// AddAsync is async purely because some value generators need to talk to
    /// the database; for Guid keys it completes synchronously. It still does
    /// not touch the database - only SaveChangesAsync does.
    /// </remarks>
    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default) =>
        await _context.Tickets.AddAsync(ticket, cancellationToken);

    /// <summary>
    /// Marks a ticket deleted. SoftDeleteInterceptor turns this into an update.
    /// </summary>
    public void Remove(Ticket ticket) => _context.Tickets.Remove(ticket);

    public async Task<IReadOnlyList<Ticket>> GetExpiredSlaTicketsAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        await _context.Tickets.Where(t => t.SlaStatus == TicketSlaStatus.Pending 
            && t.ResponseDeadlineUtc.HasValue && t.ResponseDeadlineUtc.Value <= nowUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpenTicketsPerAgent>> GetOpenTicketsPerAgentAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t=> t.Status == TicketStatus.InProgress && t.AssigneeId != null)
            .GroupBy(t => t.AssigneeId)
            .Select(group => new OpenTicketsPerAgent(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AverageResolutionTimePerCategory>> GetAverageResolutionTimePerCategoryAsync( CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t =>
                t.Status == TicketStatus.Closed &&
                t.ClosedOnUtc.HasValue)
            .GroupBy(t => t.CategoryId)
            .Select(group => new AverageResolutionTimePerCategory(
                group.Key,
                group.Average(t =>
                    EF.Functions.DateDiffMinute(t.CreatedOnUtc,t.ClosedOnUtc.Value) / 60.0)))
            .ToListAsync(cancellationToken);
    }

    public async Task<SlaBreachesThisMonth> GetSlaBreachesThisMonthAsync(
        DateTime monthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default)
    {
        var breachCount = await _context.Tickets
            .Where(t =>
                t.SlaStatus == TicketSlaStatus.Breached &&
                t.ResponseDeadlineUtc.HasValue &&
                t.ResponseDeadlineUtc.Value >= monthStartUtc &&
                t.ResponseDeadlineUtc.Value < nextMonthStartUtc)
            .CountAsync(cancellationToken);

        return new SlaBreachesThisMonth(breachCount);
    }
}
