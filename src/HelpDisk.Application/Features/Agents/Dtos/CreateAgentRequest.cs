namespace HelpDisk.Application.Features.Agents.Dtos;

public sealed record CreateAgentRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);