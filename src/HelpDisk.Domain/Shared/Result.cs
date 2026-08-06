namespace HelpDisk.Domain.Shared;

/// <summary>
/// The outcome of an operation that either succeeded or failed for a reason you
/// anticipated.
/// </summary>
/// <remarks>
/// WHY NOT JUST THROW?
///
/// Compare two signatures:
///
///     void Assign(string assigneeId);              // might throw. might not.
///                                                  // the compiler will not say.
///     Result Assign(string assigneeId);            // failure is in the type.
///                                                  // you cannot not notice.
///
/// Three concrete wins:
///
///   1. HONEST SIGNATURES. "This can fail" is visible at the call site instead
///      of buried in an XML comment nobody reads.
///
///   2. NO CONTROL FLOW BY EXCEPTION. "Ticket not found" is an ordinary,
///      expected outcome of a lookup - it happens dozens of times a day. Using
///      exceptions for it means your logs fill with noise and you cannot tell a
///      real outage from a user typo.
///
///   3. LAYER INDEPENDENCE - the big one. Domain returns
///      Error(ErrorType.NotFound). It does not return 404, because Domain must
///      not know HTTP exists. ApiController does that translation at the edge.
///      Swap the API for a gRPC service or a console app and every layer below
///      is unchanged.
///
/// The cost is real and worth stating: you must check IsFailure and propagate,
/// every time. C# has no do-notation to do it for you. Miss a check and you
/// carry on with a failed result - which is why the constructor below is
/// paranoid about impossible states.
/// </remarks>
public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        // Guard against the two states that must never exist. If either fires,
        // somebody has constructed a Result by hand instead of using the
        // factory methods - a bug that would otherwise stay silent until it
        // produced a nonsensical HTTP response.
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Lets you write <c>return TicketErrors.NotFound(id);</c> instead of
    /// <c>return Result.Failure(TicketErrors.NotFound(id));</c>.
    /// </summary>
    /// <remarks>
    /// Implicit operators are usually a smell - they hide conversions. This one
    /// earns its place because it is used on almost every guard clause in the
    /// solution, and the noise it removes is noise that would otherwise obscure
    /// the business rule sitting next to it.
    /// </remarks>
    public static implicit operator Result(Error error) => Failure(error);
}
