using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Agents.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Agents;

public interface IAgentService
{
    Task<Result<string>> CreateAsync(
        CreateAgentRequest request,
        CancellationToken cancellationToken = default);
    Task<Result<UserInfo>> UpdateAsync(
    string userId,
    UpdateAgentRequest request,
    CancellationToken cancellationToken = default);

    Task<Result<List<UserInfo>>> GetAllAsync(
    CancellationToken cancellationToken = default);

    Task<Result<UserInfo>> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(
        string userId,
        CancellationToken cancellationToken = default);
}