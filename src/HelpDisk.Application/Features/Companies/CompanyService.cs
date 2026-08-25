using HelpDisk.Application.Features.Companies.Dtos;
using HelpDisk.Domain.Companies;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Companies;

public sealed class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;

    public CompanyService(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result<IReadOnlyList<CompanyResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companyRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<CompanyResponse> response = companies
            .Select(c => new CompanyResponse(c.Id, c.Name))
            .ToList();

        return Result.Success(response);
    }
}
