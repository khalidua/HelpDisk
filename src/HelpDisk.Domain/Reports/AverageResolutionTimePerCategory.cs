namespace HelpDisk.Domain.Reports;

public sealed record AverageResolutionTimePerCategory(
    Guid CategoryId,
    double AverageResolutionTimeInHours);