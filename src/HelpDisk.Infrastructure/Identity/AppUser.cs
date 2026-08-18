using Microsoft.AspNetCore.Identity;
namespace HelpDisk.Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

}