using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.Infrastructure.Persistence;

/// <summary>
/// The EF Core session. The single place that knows this application stores
/// data in SQL Server.
/// </summary>
/// <remarks>
/// Note that nothing in Application or Domain has ever mentioned this type.
/// Services depend on ITicketRepository and IUnitOfWork; only the concrete
/// classes in this folder touch a DbContext.
///
/// The DbSets are expression-bodied (=> Set&lt;T&gt;()) rather than auto-properties
/// with setters. Same behaviour, but nobody can reassign them, and there is no
/// nullable-reference warning to suppress with "= null!".
/// </remarks>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this assembly, so
        // adding an entity means adding a configuration file and nothing else.
        //
        // The alternative - configuring every entity inline here - produces one
        // enormous method that every developer edits, and therefore every
        // developer conflicts on.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
