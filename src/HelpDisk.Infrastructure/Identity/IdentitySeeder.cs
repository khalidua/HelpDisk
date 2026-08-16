using Microsoft.AspNetCore.Identity;

namespace HelpDisk.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    [
        "Customer",
        "Agent",
        "Admin"
    ];
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager)
    {
        // Seed roles
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}'.");
                }
            }
        }

        // Seed admin
        const string adminEmail = "admin@helpdisk.com";
        const string adminPassword = "Admin123!";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator"
            };

            var result = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin: {string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description))}");
            }
        }

        // Make sure the admin has the Admin role
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(
                admin,
                "Admin");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Admin role to the admin user.");
            }
        }
    }
}