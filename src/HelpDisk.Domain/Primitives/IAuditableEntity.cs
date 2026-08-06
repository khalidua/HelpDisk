namespace HelpDisk.Domain.Primitives;

/// <summary>
/// An entity that records when it was created and last changed.
/// </summary>
/// <remarks>
/// Nothing in Application or Domain ever assigns these. They are stamped
/// automatically by AuditableEntityInterceptor in the Infrastructure layer,
/// which runs inside SaveChangesAsync.
///
/// That is the lesson here: auditing is a cross-cutting concern. If every
/// service method had to remember "and set ModifiedOnUtc", one of them
/// eventually would not - and you would only find out months later while
/// trying to answer "who changed this and when?".
///
/// The setters are public because the interceptor needs them. Treat them as
/// off-limits everywhere else.
/// </remarks>
public interface IAuditableEntity
{
    DateTime CreatedOnUtc { get; set; }

    DateTime? ModifiedOnUtc { get; set; }
}
