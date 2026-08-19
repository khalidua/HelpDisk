namespace HelpDisk.Application.Features.Agents.Dtos;

public sealed record AgentResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive);