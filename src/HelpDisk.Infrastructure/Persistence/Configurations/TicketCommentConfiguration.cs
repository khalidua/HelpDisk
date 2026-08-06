using HelpDisk.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDisk.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TicketComment"/> to its table.
/// </summary>
/// <remarks>
/// A comment is part of the Ticket aggregate, so its table exists but nothing
/// queries it directly - TicketRepository always reaches comments through their
/// ticket. AppDbContext.TicketComments is present because EF wants the entity
/// registered, not as an invitation to query it.
///
/// Note there is no query filter here even though Ticket has one. Comments do
/// not implement ISoftDeleteEntity: deleting a ticket cascades to its comments
/// at the database level, and a comment has no independent life to be soft
/// deleted from.
/// </remarks>
public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");

        builder.HasKey(c => c.Id);

        // ---------------------------------------------------------------------
        // ValueGeneratedNever() - the line whose absence produced a real bug
        // ---------------------------------------------------------------------
        // Ticket.AddComment creates the comment with Guid.NewGuid(). The DOMAIN
        // generates the identity, not the database.
        //
        // EF does not know that. Its convention for a Guid key is
        // ValueGeneratedOnAdd - "the store supplies this". That assumption feeds
        // a heuristic EF applies to any untracked entity it discovers through a
        // tracked parent's navigation:
        //
        //     key is store-generated AND already has a value
        //         => this must be an existing row => mark it Modified
        //
        // A new comment added to a loaded ticket goes down exactly that path, so
        // EF issued:
        //
        //     UPDATE [TicketComments] SET ... WHERE [Id] = @p5      -- 0 rows
        //
        // and threw DbUpdateConcurrencyException ("expected to affect 1 row(s),
        // but actually affected 0"). A baffling error whose real cause is three
        // steps away from the message.
        //
        // Ticket itself never hit this, because TicketRepository.AddAsync sets
        // EntityState.Added explicitly. Only entities discovered THROUGH A
        // NAVIGATION are inferred - which is to say, precisely the children of
        // an aggregate.
        //
        // ValueGeneratedNever() states the truth: identity comes from the domain.
        // Say it on every entity whose factory calls Guid.NewGuid().
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(TicketComment.BodyMaxLength);

        builder.Property(c => c.AuthorId)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(c => c.TicketId);
    }
}
