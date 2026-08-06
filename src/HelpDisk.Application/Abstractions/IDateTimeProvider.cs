namespace HelpDisk.Application.Abstractions;

/// <summary>
/// The current time, as a dependency rather than a static call.
/// </summary>
/// <remarks>
/// DateTime.UtcNow is a hidden input. A method that calls it cannot be tested
/// for "what happens on the last day of the month" or "what happens when this
/// SLA expires" without waiting for the calendar. Injecting the clock makes
/// time an ordinary parameter you can control.
///
/// Used sparingly here, and honestly: Ticket.Close() calls DateTime.UtcNow
/// directly rather than taking this as a parameter, and that file explains the
/// trade-off. The abstraction exists for the Application layer, where
/// time-dependent logic tends to accumulate (due dates, SLA breaches, "tickets
/// older than 30 days").
///
/// .NET 8 added System.TimeProvider, which does this job in the BCL and is what
/// you would reach for on a real project. This interface is kept because one
/// two-line file is easier to explain than a framework type, and the point
/// being taught is the inversion, not the API.
/// </remarks>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
