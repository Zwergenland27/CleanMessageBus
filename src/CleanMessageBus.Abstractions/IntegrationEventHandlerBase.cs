using CleanDomainValidation.Domain;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Defines handler for events of type <typeparamref name="TIntegrationEvent"/>
/// </summary>
public abstract class IntegrationEventHandlerBase<TIntegrationEvent> : IRequestHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Actual event handler logic
    /// </summary>
    /// <param name="event">Incoming event object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task<CanFail> Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
}