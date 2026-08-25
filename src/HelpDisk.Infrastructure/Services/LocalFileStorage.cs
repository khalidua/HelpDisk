using HelpDisk.Application.Abstractions;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Infrastructure.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage()
    {
        _rootPath = Path.Combine(
            AppContext.BaseDirectory,
            "uploads",
            "attachments");

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<Result<string>> SaveAsync(
        Stream file,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

            var fullPath = Path.Combine(
                _rootPath,
                storageKey);

            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(
                output,
                cancellationToken);

            return Result.Success(storageKey);
        }
        catch
        {
            return FileStorageErrors.SaveFailed;
        }
    }

    public Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(
                _rootPath,
                storageKey);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Result<Stream>>(
                    FileStorageErrors.FileNotFound);
            }

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return Task.FromResult(
                Result.Success(stream));
        }
        catch
        {
            return Task.FromResult<Result<Stream>>(
                FileStorageErrors.FileNotFound);
        }
    }

    public Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(
                _rootPath,
                storageKey);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.FromResult(Result.Success());
        }
        catch
        {
            return Task.FromResult<Result>(
                FileStorageErrors.DeleteFailed);
        }
    }
}