using CleanDomainValidation.Domain;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Defines handler for events of type <typeparamref name="TDomainEvent"/>
/// </summary>
public abstract class DomainEventHandlerBase<TDomainEvent> : IRequestHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Actual event handler logic
    /// </summary>
    /// <param name="event">Incoming event object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task<CanFail> Handle(TDomainEvent @event, CancellationToken cancellationToken);
}