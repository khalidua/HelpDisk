using HelpDisk.Domain.Tickets;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDisk.Infrastructure.Persistence.Configurations;

public sealed class TicketAttachmentConfiguration
    : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TicketId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(TicketAttachment.FileNameMaxLength);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(TicketAttachment.ContentTypeMaxLength);

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .IsRequired()
            .HasMaxLength(TicketAttachment.StorageKeyMaxLength);

        builder.Property(x => x.UploadedById)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ModifiedOnUtc)
            .IsRequired(false);
    }
}