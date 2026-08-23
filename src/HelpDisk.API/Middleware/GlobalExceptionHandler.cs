using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDisk.API.Middleware;

/// <summary>
/// Last line of defence: turns an unhandled exception into a clean 500.
/// </summary>
/// <remarks>
/// ============================================================================
/// THE DIVISION OF LABOUR WITH Result - the point of having both.
/// ============================================================================
///
///   Result             failures you PREDICTED. Ticket not found. Cannot assign
///                      a closed ticket. Validation failed. These are business
///                      OUTCOMES: the system worked correctly and the answer
///                      was no. They travel as values and become 4xx.
///
///   Exception (here)   failures you did NOT predict. A null reference. The
///                      database is down. A bug. These are not outcomes, they
///                      are the system failing to work at all. They travel as
///                      exceptions and become 500.
///
/// Keeping them separate is what makes both useful. If "not found" threw, your
/// logs would fill with routine 404s and a real outage would be invisible in
/// the noise. If a dropped connection returned a Result, every caller would
/// have to handle a failure it can do nothing about.
///
/// The rule of thumb: if a competent user could cause it by using the system
/// normally, it is a Result. If it means something is broken, let it throw.
///
/// ---------------------------------------------------------------------------
/// IExceptionHandler is the modern (.NET 8+) replacement for a try/catch
/// middleware. Register it with AddExceptionHandler + UseExceptionHandler.
/// Returning true means "handled, stop here"; false would pass to the next
/// handler in the chain.
/// ---------------------------------------------------------------------------
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception object itself, not exception.Message. Passing the
        // exception preserves the stack trace and any inner exceptions; logging
        // only the message throws away everything you would actually need at
        // 3am.
        if (exception is DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                exception,
                "Concurrency conflict for {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var concurrencyProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ticket.ConcurrencyConflict",
                Detail = "The ticket was modified by another user. Please refresh and try again.",
                Instance = httpContext.Request.Path
            };

            concurrencyProblemDetails.Extensions["traceId"] =
                httpContext.TraceIdentifier;

            httpContext.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await httpContext.Response.WriteAsJsonAsync(
                concurrencyProblemDetails,
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",

            // Deliberately vague. The exception message may contain a
            // connection string, a file path, or a SQL fragment - all of which
            // are gifts to an attacker. Details go to the log; the caller gets
            // an apology and a trace id.
            Detail = "The request could not be completed. Please try again, and quote the traceId if the problem persists.",
            Instance = httpContext.Request.Path
        };

        // The trace id lets somebody match this response to the log entry above.
        // Without it, "it broke at about 2pm" is all support will ever have.
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
