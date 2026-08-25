using HelpDisk.Application.Features.Companies.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Companies;

public interface ICompanyService
{
    Task<Result<IReadOnlyList<CompanyResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
}
