
using HelpDisk.Domain.Companies;
using HelpDisk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Auth;
using HelpDisk.Domain.Shared;

using Microsoft.AspNetCore.Identity;

namespace HelpDisk.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public IdentityService(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }
    public async Task<Result<string>> CreateCustomerAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var companyExists = await _context.Set<Company>()
        .AnyAsync(c => c.Id == companyId, cancellationToken);

        if (!companyExists)
        {
            return AuthErrors.CompanyNotFound;
        }

        var customer = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId
        };
        var result = await _userManager.CreateAsync(customer, password);

        if (!result.Succeeded)
        {
            return AuthErrors.RegistrationFailed;
        }

        var roleResult = await _userManager.AddToRoleAsync(customer, "Customer");

        if (!roleResult.Succeeded)
        {
            return AuthErrors.RegistrationFailed;
        }

        return Result.Success(customer.Id);
    }

    public async Task<Result<UserInfo>> GetUserAsync(
       string userId,
       CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return Result.Success(
            new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                role, 
                user.CompanyId,
                user.IsActive));
    }

    public async Task<Result<UserInfo>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if(user == null)
        {
            return AuthErrors.InvalidCredentials;
        }
        if (!user.IsActive)
        {
            return AuthErrors.InvalidCredentials;
        }
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return AuthErrors.InvalidCredentials;
        }
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        return Result.Success(new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, role, user.CompanyId, user.IsActive));
    }

    public async Task<Result<string>> CreateAgentAsync(string email, string password,string firstName,string lastName,
    CancellationToken cancellationToken = default)
    {
        var agent = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await _userManager.CreateAsync(agent, password);

        if (!result.Succeeded)
        {
            return AuthErrors.RegistrationFailed;
        }

        var roleResult = await _userManager.AddToRoleAsync(agent, "Agent");

        if (!roleResult.Succeeded)
        {
            return AuthErrors.RegistrationFailed;
        }

        return Result.Success(agent.Id);
    }

    public async Task<Result<List<UserInfo>>> GetAgentsAsync(
        CancellationToken cancellationToken = default)
    {
        var agents = await _userManager.GetUsersInRoleAsync("Agent");

        var result = new List<UserInfo>();

        foreach (var agent in agents)
        {
            result.Add(
                new UserInfo(
                    agent.Id,
                    agent.Email,
                    agent.FirstName,
                    agent.LastName,
                    "Agent",
                    agent.CompanyId,
                    agent.IsActive));
        }

        return Result.Success(result);
    }

    public async Task<Result<UserInfo>> UpdateAgentAsync(
    string userId,
    string email,
    string firstName,
    string lastName,
    CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var isAgent = await _userManager.IsInRoleAsync(user, "Agent");

        if (!isAgent)
        {
            return IdentityErrors.UserNotFound;
        }

        user.Email = email;
        user.UserName = email;
        user.FirstName = firstName;
        user.LastName = lastName;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return IdentityErrors.UserUpdateFailed;
        }

        return Result.Success(
            new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                "Agent",
                user.CompanyId,
                user.IsActive));
    }

    public async Task<Result<UserInfo>> GetAgentAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var isAgent = await _userManager.IsInRoleAsync(user, "Agent");

        if (!isAgent)
        {
            return IdentityErrors.UserNotFound;
        }

        return Result.Success(
            new UserInfo(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                "Agent",
                user.CompanyId,
                user.IsActive));
    }

    public async Task<Result> DeactivateAgentAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var isAgent = await _userManager.IsInRoleAsync(user, "Agent");

        if (!isAgent)
        {
            return IdentityErrors.UserNotFound;
        }

        user.IsActive = false;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return IdentityErrors.UserUpdateFailed;
        }

        return Result.Success();
    }

    public async Task<Result> ActivateAgentAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var isAgent = await _userManager.IsInRoleAsync(user, "Agent");

        if (!isAgent)
        {
            return IdentityErrors.UserNotFound;
        }

        user.IsActive = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return IdentityErrors.UserUpdateFailed;
        }

        return Result.Success();
    }
}
