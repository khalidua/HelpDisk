using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Categories;
using HelpDisk.Application.Features.Categories.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

/// <summary>
/// HTTP endpoints for the Category feature.
/// </summary>
/// <remarks>
/// The same shape as TicketsController, at a quarter of the size. Create a
/// category first - a ticket cannot be created without one.
/// </remarks>
[Route("api/categories")]
public sealed class CategoriesController : ApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// <summary>Creates a category.</summary>
    /// <remarks>Returns 409 if a category with the same name already exists.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Lists all categories, alphabetically.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(cancellationToken);
        return HandleResult(result);
    }
}
