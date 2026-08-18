namespace HelpDisk.Domain.Reports;

public sealed record OpenTicketsPerAgent(
    string? AgentId,
    int OpenTicketsCount);