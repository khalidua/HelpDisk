using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Abstractions;

public interface IIdentityService
{
    Task<Result<string>> CreateCustomerAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid companyId,
        CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> GetUserAsync(
    string userId,
    CancellationToken cancellationToken = default);
}
