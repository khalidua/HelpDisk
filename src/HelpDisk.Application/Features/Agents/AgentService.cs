using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Agents.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Agents;

public sealed class AgentService : IAgentService
{
    private readonly IIdentityService _identityService;

    public AgentService(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<string>> CreateAsync(
        CreateAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityService.CreateAgentAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        return result;
    }

    public async Task<Result<UserInfo>> UpdateAsync(
    string userId,
    UpdateAgentRequest request,
    CancellationToken cancellationToken = default)
    {
        var result = await _identityService.UpdateAgentAsync(
            userId,
            request.Email,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == IdentityErrors.UserNotFound)
            {
                return AgentErrors.AgentNotFound;
            }

            return AgentErrors.AgentUpdateFailed;
        }

        return result;
    }

    public async Task<Result<List<UserInfo>>> GetAllAsync(
    CancellationToken cancellationToken = default)
    {
        return await _identityService.GetAgentsAsync(cancellationToken);
    }

    public async Task<Result<UserInfo>> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityService.GetAgentAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == IdentityErrors.UserNotFound)
            {
                return AgentErrors.AgentNotFound;
            }

            return result.Error;
        }

        return result;
    }

    public async Task<Result> DeactivateAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityService.DeactivateAgentAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == IdentityErrors.UserNotFound)
            {
                return AgentErrors.AgentNotFound;
            }

            return AgentErrors.AgentDeactivationFailed;
        }

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityService.ActivateAgentAsync(
            userId,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == IdentityErrors.UserNotFound)
            {
                return AgentErrors.AgentNotFound;
            }

            return AgentErrors.AgentActivationFailed;
        }

        return Result.Success();
    }
}