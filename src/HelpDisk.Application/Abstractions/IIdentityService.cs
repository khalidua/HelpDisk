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
    Task<Result<string>> CreateAgentAsync(
    string email,
    string password,
    string firstName,
    string lastName,
    CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> UpdateAgentAsync(
    string userId,
    string email,
    string firstName,
    string lastName,
    CancellationToken cancellationToken = default);

    Task<Result<List<UserInfo>>> GetAgentsAsync(
    CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> GetAgentAsync(
    string userId,
    CancellationToken cancellationToken = default);

    Task<Result> DeactivateAgentAsync(
    string userId,
    CancellationToken cancellationToken = default);

    Task<Result> ActivateAgentAsync(
    string userId,
    CancellationToken cancellationToken = default);

}
