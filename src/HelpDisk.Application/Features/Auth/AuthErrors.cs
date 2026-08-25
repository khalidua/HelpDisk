using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error RegistrationFailed = Error.Validation(
        "Auth.RegistrationFailed",
        "The user could not be registered.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error UserNotFound = Error.NotFound(
        "Auth.UserNotFound",
        "The specified user was not found.");

    public static readonly Error CompanyNotFound = Error.NotFound(
        "Auth.CompanyNotFound",
        "The specified company was not found.");
}