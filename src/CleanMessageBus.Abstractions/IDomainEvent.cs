using CleanDomainValidation.Application;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : IRequest;