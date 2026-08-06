namespace HelpDisk.Domain.Shared;

/// <summary>
/// A <see cref="Result"/> that also carries a value when it succeeds.
/// </summary>
/// <remarks>
/// Use Result&lt;T&gt; when the caller needs something back (the new ticket's Id,
/// the ticket itself, a page of results) and plain Result when the operation is
/// a command with nothing to return (Close, Delete).
/// </remarks>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The value produced on success.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if read on a failed result.
    /// </exception>
    /// <remarks>
    /// This throws rather than returning null, and that is deliberate. Reading
    /// Value without checking IsSuccess is a programming mistake, not a runtime
    /// condition - so it should fail loudly and immediately, at the line that
    /// is wrong, instead of handing back a null that blows up three frames
    /// later somewhere unrelated.
    ///
    /// (The MOJ reference codebase returns default! here instead. That silently
    /// produces a null and moves the crash somewhere less useful.)
    /// </remarks>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>
    /// Lets a method declared as Result&lt;Ticket&gt; simply <c>return ticket;</c>.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    /// Lets a method declared as Result&lt;Ticket&gt; simply
    /// <c>return TicketErrors.NotFound(id);</c>.
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
