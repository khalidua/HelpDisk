using HelpDisk.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDisk.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Category"/> to its table.
/// </summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        // Identity comes from Category.Create. See TicketCommentConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        // CategoryService.CreateAsync already checks for a duplicate name, so
        // why also index it as unique? Because that check is a read followed by
        // a write, and two requests can pass the read before either writes.
        //
        // The service check exists to give a friendly 409. The unique index
        // exists to make the rule actually true. Application-level checks are
        // for humans; database constraints are for correctness. Under real
        // concurrency you want both.
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.ResponseTimeTargetHours).IsRequired();
    }
}
