namespace HelpDisk.Application.Features.Categories.Dtos;

public sealed record CreateCategoryRequest(string Name);

public sealed record CategoryResponse(Guid Id, string Name, DateTime CreatedOnUtc);
