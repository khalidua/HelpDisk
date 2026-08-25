namespace HelpDisk.Domain.Companies;

public interface ICompanyRepository
{
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);
}
