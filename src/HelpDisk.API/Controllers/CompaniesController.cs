using HelpDisk.API.Abstractions;
using HelpDisk.Application.Features.Companies;
using HelpDisk.Application.Features.Companies.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Controllers;

/// <summary>
/// HTTP endpoints for Companies.
/// </summary>
[AllowAnonymous]
[Route("api/companies")]
public sealed class CompaniesController : ApiController
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>Lists all companies, alphabetically.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _companyService.GetAllAsync(cancellationToken);
        return HandleResult(result);
    }
}
