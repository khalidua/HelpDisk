using HelpDisk.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HelpDisk.API.Abstractions;

/// <summary>
/// Base controller that turns a <see cref="Result"/> into an HTTP response.
/// </summary>
/// <remarks>
/// ============================================================================
/// THIS CLASS IS THE SEAM. It is the only place in the entire solution that
/// knows HTTP status codes exist.
/// ============================================================================
///
/// Follow a "ticket not found" through the layers:
///
///   Domain          TicketErrors.NotFound(id)   -> Error with ErrorType.NotFound
///   Application     returns that Error in a Result
///   API (here)      ErrorType.NotFound          -> 404
///
/// Domain never said "404". Application never said "404". They both said "this
/// was not found", which is a fact about the business, and the outermost layer
/// translated it into the vocabulary of the transport it happens to speak.
///
/// That is what makes the claim "you could put a different front end on this"
/// real rather than aspirational. A gRPC service would map ErrorType.NotFound
/// to StatusCode.NotFound. A console app would print a message. Neither would
/// require touching a single line of Application or Domain - and the reason is
/// that the mapping lives in exactly one file, this one.
///
/// If you ever find a status code, an IActionResult, or a HttpContext outside
/// the API project, the seam has torn.
/// </remarks>
[ApiController]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    /// <summary>Handles a result with no value - a command that just succeeded.</summary>
    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess
            ? Ok()
            : Problem(result.Error);

    /// <summary>Handles a result carrying a value.</summary>
    protected IActionResult HandleResult<TValue>(Result<TValue> result) =>
        result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error);

    /// <summary>
    /// Builds an RFC 7807 ProblemDetails response from a domain error.
    /// </summary>
    /// <remarks>
    /// ProblemDetails is the standard shape for HTTP error bodies, so clients
    /// and tooling know what to expect:
    ///
    ///     {
    ///       "type":   "https://tools.ietf.org/html/rfc9110#section-15.5.5",
    ///       "title":  "Ticket.CannotAssignClosed",
    ///       "status": 409,
    ///       "detail": "A closed ticket cannot be assigned. Reopen it first."
    ///     }
    ///
    /// The Error CODE goes in "title" because it is the stable, machine-readable
    /// key a client should branch on. The DESCRIPTION goes in "detail" because
    /// it is the human-readable sentence, and it may be reworded at any time.
    /// </remarks>
    private IActionResult Problem(Error error) =>
        Problem(
            statusCode: ToStatusCode(error.Type),
            title: error.Code,
            detail: error.Description);

    /// <summary>
    /// The single translation table from domain failure categories to HTTP.
    /// </summary>
    /// <remarks>
    /// Why each one:
    ///
    ///   NotFound     404 - the thing does not exist.
    ///   Validation   400 - the request was malformed. Fix the payload and retry.
    ///   Conflict     409 - the request was fine, but the resource is in a state
    ///                      that forbids it. Retrying the same payload will fail
    ///                      again until something else changes. THIS is why
    ///                      Conflict and Validation are separate categories -
    ///                      the distinction tells the caller whether editing
    ///                      their input would help.
    ///   Unauthorized 401 - we do not know who you are.
    ///   Forbidden    403 - we know who you are, and you may not do this.
    ///
    /// The default arm is defensive: an ErrorType with no mapping is a bug in
    /// this switch, and 500 is the honest answer to "the server does not know
    /// what it meant".
    /// </remarks>
    private static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
