using HelpDisk.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.Infrastructure.Persistence.Repositories;

/// <summary>
/// The EF Core implementation of <see cref="ICategoryRepository"/>.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Used by TicketService before creating a ticket.
    /// </summary>
    /// <remarks>
    /// AnyAsync, not GetByIdAsync. The question is "does it exist?", and
    /// AnyAsync answers it with SELECT 1 rather than fetching a whole row we
    /// would immediately discard. Small habit, and it is free.
    /// </remarks>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    /// <summary>
    /// Case-insensitive under SQL Server's default collation.
    /// </summary>
    /// <remarks>
    /// Worth flagging rather than relying on: "Hardware" and "hardware" count
    /// as duplicates here because the database collation says so, not because
    /// this code said so. On a case-sensitive collation the behaviour changes
    /// with no code change.
    ///
    /// Behaviour that depends on database configuration is behaviour you should
    /// write down - or pin explicitly, e.g. by comparing a normalised column.
    /// </remarks>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default) =>
        await _context.Categories.AnyAsync(
            c => c.Name == name && 
            (!excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value), cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await _context.Categories.AddAsync(category, cancellationToken);

    public async Task<Category?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default) =>
    await _context.Categories
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> HasTicketsAsync(Guid categoryId, CancellationToken cancellationToken = default) 
        => await _context.Tickets.AnyAsync(t => t.CategoryId == categoryId, cancellationToken);

    public void Remove(Category category) => _context.Categories.Remove(category);
}
