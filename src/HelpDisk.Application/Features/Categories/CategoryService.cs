using FluentValidation;
using HelpDisk.Application.Features.Categories.Dtos;
using HelpDisk.Domain.Categories;
using HelpDisk.Domain.Repositories;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Categories;

/// <summary>
/// Business logic for the Category feature.
/// </summary>
/// <remarks>
/// Same five beats as TicketService - validate, load, ask the domain, persist,
/// shape - just fewer of them. A simple feature should LOOK simple. Resist
/// adding structure here to match Tickets; symmetry for its own sake is how
/// small features acquire large folders.
///
/// One thing worth noticing: the uniqueness check below cannot live in
/// Category.Create. "Is this name already taken?" is a question about every
/// category, and a single Category instance has no way to know. Rules that need
/// to see the whole collection belong in the service (or, if they must be
/// bulletproof under concurrency, in a unique index - see CategoryConfiguration).
/// </remarks>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _createValidator;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> createValidator)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(
                "Validation.Failed",
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        // A collection-wide rule, so the service owns it - see the class remarks.
        if (await _categories.NameExistsAsync(request.Name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists;
        }

        var categoryResult = Category.Create(request.Name);
        if (categoryResult.IsFailure)
        {
            return categoryResult.Error;
        }

        await _categories.AddAsync(categoryResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return categoryResult.Value.Id;
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);

        // Mapped by hand rather than through Mapster. For a two-property record
        // a lambda is shorter than a mapping config, and shorter to explain.
        // Use a mapper when the shapes are big enough to make it worth it, not
        // because the project happens to have one.
        IReadOnlyList<CategoryResponse> response = categories
            .Select(c => new CategoryResponse(c.Id, c.Name, c.CreatedOnUtc))
            .ToList();

        return Result.Success(response);
    }
}
