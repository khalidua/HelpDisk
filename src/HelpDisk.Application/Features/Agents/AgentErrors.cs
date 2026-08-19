using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Agents;

public static class AgentErrors
{
    public static readonly Error AgentNotFound = Error.NotFound(
        "Agents.NotFound",
        "The specified agent was not found.");

    public static readonly Error AgentCreationFailed = Error.Validation(
        "Agents.CreationFailed",
        "The agent account could not be created.");

    public static readonly Error AgentUpdateFailed = Error.Validation(
        "Agents.UpdateFailed",
        "The agent account could not be updated.");

    public static readonly Error AgentActivationFailed = Error.Validation(
        "Agents.ActivationFailed",
        "The agent could not be activated.");

    public static readonly Error AgentDeactivationFailed = Error.Validation(
        "Agents.DeactivationFailed",
        "The agent could not be deactivated.");
}