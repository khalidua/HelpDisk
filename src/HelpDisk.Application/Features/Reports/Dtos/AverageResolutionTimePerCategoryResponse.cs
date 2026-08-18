namespace HelpDisk.Application.Features.Reports.Dtos;

public sealed record AverageResolutionTimePerCategoryResponse(
    Guid CategoryId,
    double AverageResolutionTimeInHours);