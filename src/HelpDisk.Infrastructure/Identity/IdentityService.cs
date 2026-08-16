using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Auth;
using HelpDisk.Domain.Shared;
using HelpDisk.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace HelpDisk.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    public IdentityService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }
    public async Task<Result<string>> CreateCustomerAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var customer = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = UserRole.Customer
        };
        var result = await _userManager.CreateAsync(customer, password);

        if (!result.Succeeded)
        {
            return AuthErrors.RegistrationFailed;
        }

        return Result.Success(customer.Id);
    }

    public async Task<Result<UserInfo>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if(user == null)
        {
            return AuthErrors.InvalidCredentials;
        }
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return AuthErrors.InvalidCredentials;
        }
        
        return Result.Success(new UserInfo(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }
}
