namespace HelpDisk.Application.Features.Categories.Dtos;

public sealed record CreateCategoryRequest(string Name, int ResponseTimeTargetHours);

public sealed record CategoryResponse(Guid Id, string Name, int ResponseTimeTargetHours, DateTime CreatedOnUtc);

public sealed record UpdateCategoryRequest(string Name, int ResponseTimeTargetHours);

