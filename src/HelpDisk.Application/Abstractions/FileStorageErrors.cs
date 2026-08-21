using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Abstractions;

public static class FileStorageErrors
{
    public static readonly Error SaveFailed = Error.Validation(
        "FileStorage.SaveFailed",
        "The file could not be saved.");

    public static readonly Error FileNotFound = Error.NotFound(
        "FileStorage.FileNotFound",
        "The requested file could not be found.");

    public static readonly Error DeleteFailed = Error.Validation(
        "FileStorage.DeleteFailed",
        "The file could not be deleted.");
}