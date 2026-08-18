using HelpDisk.Domain.Companies;
using HelpDisk.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDisk.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne<Company>()
            .WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);
    }
}
