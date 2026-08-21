using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Abstractions;

public interface IFileStorage
{
    Task<Result<string>> SaveAsync(
        Stream file,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}