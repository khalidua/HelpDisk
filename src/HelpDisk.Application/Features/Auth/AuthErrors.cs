using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error RegistrationFailed = Error.Validation(
        "Auth.RegistrationFailed",
        "The user could not be registered.");

    public static readonly Error InvalidCredentials = Error.Validation(
        "Auth.InvalidCredentials",
        "Invalid email or password.");
}