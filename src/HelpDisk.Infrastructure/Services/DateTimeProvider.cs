using HelpDisk.Application.Abstractions;

namespace HelpDisk.Infrastructure.Services;

/// <summary>
/// The real clock.
/// </summary>
/// <remarks>
/// One line of code and a whole class, which looks like ceremony until you need
/// to test something time-dependent. A test registers a fake returning a fixed
/// instant, and "what happens when this SLA expires?" becomes an ordinary
/// assertion instead of a two-hour wait.
///
/// Registered as a singleton: it holds no state, so one instance serves every
/// request.
/// </remarks>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
