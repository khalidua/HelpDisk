using Microsoft.AspNetCore.Identity;

using HelpDisk.Domain.Users;

namespace HelpDisk.Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}