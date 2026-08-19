using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Abstractions;

public static class IdentityErrors
{
    public static readonly Error UserNotFound = Error.NotFound(
        "Identity.UserNotFound",
        "The specified user was not found.");

    public static readonly Error UserCreationFailed = Error.Validation(
        "Identity.UserCreationFailed",
        "The user account could not be created.");

    public static readonly Error UserUpdateFailed = Error.Validation(
        "Identity.UserUpdateFailed",
        "The user account could not be updated.");
}