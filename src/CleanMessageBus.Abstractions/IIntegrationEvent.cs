using CleanDomainValidation.Application;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Marker interface for integration events
/// </summary>
public interface IIntegrationEvent : IRequest;