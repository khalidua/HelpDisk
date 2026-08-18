using HelpDisk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDisk.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Ticket"/> to its table.
/// </summary>
/// <remarks>
/// ============================================================================
/// THIS FILE IS WHERE A RICH AGGREGATE MEETS ITS ORM. Read the two commented
/// blocks below - they are the practical cost of the design, and the two things
/// people get wrong.
/// ============================================================================
///
/// The mapping lives HERE and not on the entity. There are no [Table],
/// [Column], [MaxLength] or [Required] attributes on Ticket, and that is
/// deliberate: attributes would put EF Core types on a Domain class and undo
/// the independence the layer exists for. Fluent configuration keeps every
/// persistence decision on this side of the wall.
/// </remarks>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        // Identity comes from Ticket.Create (Guid.NewGuid()), not the database.
        // See TicketCommentConfiguration for the bug that omitting this causes -
        // it bites hardest on aggregate children, but state it everywhere the
        // domain generates the key, so the model is consistent and nobody has to
        // remember which entities are special.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(Ticket.TitleMaxLength);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(Ticket.DescriptionMaxLength);

        // Enums are stored as int. Explicit conversion so the intent is on the
        // record - and so a future change to string storage is a one-line edit
        // here rather than a hunt through conventions.
        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.ReporterId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.AssigneeId)
            .HasMaxLength(128);

        // ---------------------------------------------------------------------
        // THE CATEGORY RELATIONSHIP - a foreign key with no navigation property
        // ---------------------------------------------------------------------
        // Ticket has CategoryId but deliberately no "Category Category"
        // property (see Ticket.cs for why). EF can still enforce the foreign
        // key: the generic HasOne<Category>() with no argument means "related
        // to Category, but there is no navigation to it on this side", and
        // WithMany() likewise means "and none on the other side either".
        //
        // DeleteBehavior.Restrict stops a category from being deleted while
        // tickets still reference it. The default (Cascade) would silently
        // delete every ticket in a category the moment somebody tidied up the
        // lookup list.
        builder.HasOne<Domain.Categories.Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------------------------------------------------------------------
        // THE COMMENTS COLLECTION - the line people forget
        // ---------------------------------------------------------------------
        // Ticket.Comments is a read-only view over the private _comments field.
        // Without the SetPropertyAccessMode line below, EF tries to write
        // through the PROPERTY - which returns a fresh ReadOnlyCollection
        // wrapper each call, so anything EF adds lands in a throwaway object.
        //
        // The failure mode is nasty precisely because it is quiet: no
        // exception, no warning, comments simply never load and never save. You
        // find out from a user.
        //
        // PropertyAccessMode.Field tells EF to read and write _comments
        // directly and never touch the getter.
        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Ticket.Comments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // ---------------------------------------------------------------------
        // SOFT DELETE - the global query filter
        // ---------------------------------------------------------------------
        // Appends "WHERE IsDeleted = 0" to every query against Tickets,
        // automatically and everywhere. This is the half of soft delete that
        // makes it invisible to callers; SoftDeleteInterceptor is the other.
        //
        // Worth knowing: IgnoreQueryFilters() opts out when an admin screen
        // genuinely needs deleted rows.
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Indexes matching how TicketRepository.SearchAsync actually filters.
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.CreatedOnUtc);

        // Domain events live in memory only - they are dispatched and dropped,
        // never persisted. Without this, EF sees a collection property it does
        // not recognise and fails to build the model.
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.ResponseDeadlineUtc).IsRequired(false);

        builder.Property(t => t.SlaStatus).HasConversion<int>().IsRequired();
    }
}
