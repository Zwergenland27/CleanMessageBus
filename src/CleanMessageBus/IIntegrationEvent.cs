using CleanDomainValidation.Application;

namespace CleanMessageBus;

/// <summary>
/// Marker interface for integration events
/// </summary>
public interface IIntegrationEvent : IRequest;