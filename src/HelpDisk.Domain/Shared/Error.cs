namespace HelpDisk.Domain.Shared;

/// <summary>
/// A described, categorised failure. The alternative to throwing.
/// </summary>
/// <param name="Code">
/// A stable machine-readable key, e.g. "Ticket.CannotAssignClosed". Front-end
/// code and translation files key off this, so treat it as part of your public
/// contract: changing it breaks callers just as surely as renaming an endpoint.
/// </param>
/// <param name="Type">
/// The CATEGORY of failure. This is what lets ApiController translate a
/// domain failure into an HTTP status code without Domain ever knowing HTTP
/// exists. It is the seam that keeps the layers apart.
/// </param>
/// <param name="Description">A human-readable explanation, safe to show a user.</param>
public sealed record Error(string Code, ErrorType Type, string Description = "")
{
    /// <summary>
    /// The absence of an error. A successful Result always carries this.
    /// </summary>
    public static readonly Error None = new(string.Empty, ErrorType.None);

    public static Error NotFound(string code, string description) =>
        new(code, ErrorType.NotFound, description);

    public static Error Validation(string code, string description) =>
        new(code, ErrorType.Validation, description);

    public static Error Conflict(string code, string description) =>
        new(code, ErrorType.Conflict, description);

    public static Error Forbidden(string code, string description) =>
        new(code, ErrorType.Forbidden, description);

    public static Error Unauthorized(string code, string description) =>
        new(code, ErrorType.Unauthorized, description);
}

/// <summary>
/// The kinds of failure the system distinguishes.
/// </summary>
/// <remarks>
/// Keep this list SHORT. Every value here must map to exactly one HTTP status
/// code in ApiController, and a long list means the mapping becomes a guess.
///
/// Note what is missing: there is no ErrorType.Unexpected. Unexpected failures
/// are not Results - a null reference or a dropped database connection is not a
/// business outcome you model, it is a bug or an outage. Those throw, and
/// GlobalExceptionHandler turns them into a 500.
///
/// The rule: Result is for failures you PREDICTED. Exceptions are for the ones
/// you did not.
/// </remarks>
public enum ErrorType
{
    None = 0,
    NotFound,
    Validation,
    Conflict,
    Forbidden,
    Unauthorized
}
