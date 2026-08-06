using HelpDisk.Domain.Primitives;

namespace HelpDisk.Application.Abstractions.Events;

/// <summary>
/// Reacts to something that happened in the domain.
/// </summary>
/// <remarks>
/// Implement this once per (event, reaction) pair. Several handlers may listen
/// to the same event and they run independently - that is the whole appeal:
/// adding a reaction means adding a class, not editing a service.
///
/// The interface is declared in Application, not Domain, on purpose. Domain
/// RAISES events - that is a statement of fact, and it needs the event type
/// only. Deciding what to DO about a fact is a use-case concern, and use cases
/// live in Application.
///
/// This is a smaller cousin of MediatR's INotificationHandler. If you have used
/// MediatR you will recognise the shape; the difference is that here you can
/// read the entire dispatch mechanism in one file
/// (Infrastructure/Services/DomainEventDispatcher.cs) instead of trusting a
/// library.
/// </remarks>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
