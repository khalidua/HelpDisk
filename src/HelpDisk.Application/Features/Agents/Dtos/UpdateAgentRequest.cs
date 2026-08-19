namespace HelpDisk.Application.Features.Agents.Dtos;

public sealed record UpdateAgentRequest(
    string Email,
    string FirstName,
    string LastName);