using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Categories;
using HelpDisk.Application.Features.Categories.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

/// <summary>
/// HTTP endpoints for the Category feature.
/// </summary>
/// <remarks>
/// The same shape as TicketsController, at a quarter of the size. Create a
/// category first - a ticket cannot be created without one.
/// </remarks>
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController : ApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// <summary>Creates a category.</summary>
    /// <remarks>Returns 409 if a category with the same name already exists.</remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")]
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

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
    Guid categoryId,
    [FromBody] UpdateCategoryRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(
            categoryId,
            request,
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
    Guid categoryId,
    CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(
            categoryId,
            cancellationToken);

        return HandleResult(result);
    }

}
