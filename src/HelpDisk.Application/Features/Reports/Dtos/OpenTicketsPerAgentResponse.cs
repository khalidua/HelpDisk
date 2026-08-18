namespace HelpDisk.Application.Features.Reports.Dtos;

public sealed record OpenTicketsPerAgentResponse(
    string? AgentId,
    int OpenTicketsCount);