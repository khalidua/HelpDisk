using HelpDisk.Application.Features.Categories.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Categories;

/// <summary>
/// The Category feature. Two operations, and that is the whole feature.
/// </summary>
/// <remarks>
/// This slice exists so you can see the pattern TWICE. One example is an
/// anecdote; two is a pattern - and the second one is where people stop
/// copying and start understanding.
///
/// Follow docs/adding-a-feature.md and you will reproduce exactly this. If the
/// checklist ever stops matching these files, the checklist is the thing that
/// is wrong.
/// </remarks>
public interface ICategoryService
{
    Task<Result<Guid>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
