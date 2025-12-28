using System.Reflection;
using CleanMessageBus.Abstractions.Attributes;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Extension methods to centralize naming
/// </summary>
public static class NamingExtensions
{
    /// <summary>
    /// Extracts the producer name from event
    /// </summary>
    /// <param name="eventType">Type of the event</param>
    /// <returns>Name of the producer, either custom or automatically generated</returns>
    public static string GetProducerName(this Type eventType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEvent)) &&
            !eventType.IsAssignableTo(typeof(IDomainEvent)))
        {
            throw new InvalidOperationException($"Event type must be assignable to {nameof(IIntegrationEvent)} or {nameof(IDomainEvent)}");
        }
        
        var producesAttribute = eventType
            .GetCustomAttribute<ProducesAttribute>(false);

        if (producesAttribute is not null)
        {
            return producesAttribute.Name;
        }
       
        var namespaceName = eventType.Namespace!;
        var typeName = eventType.Name;
        
        return $"{namespaceName}:{typeName}";
    }

    /// <summary>
    /// Extracts the name of the producing event for an event handler
    /// </summary>
    /// <param name="eventHandlerType">Type of the event handler</param>
    /// <returns>Name of the producer, either custom or automatically generated</returns>
    public static string GetProducedByName(this Type eventHandlerType)
    {
        if (!eventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>)) &&
            !eventHandlerType.IsAssignableTo(typeof(DomainEventHandlerBase<>)))
        {
            throw new InvalidOperationException($"Event handler type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name} or {typeof(DomainEventHandlerBase<>).Name}");
        }
        
        var producedByAttribute = eventHandlerType
            .GetCustomAttribute<ProducedByAttribute>(false);

        if (producedByAttribute is not null && eventHandlerType.IsAssignableTo(typeof(DomainEventHandlerBase<>)))
        {
            throw new InvalidOperationException("ProducedBy attribute cannot be set for domain event handlers. The correct producer name will be automatically generated from the domain event type.");
        }
        
        if (producedByAttribute is not null)
        {
            return producedByAttribute.Name;
        }
        
        var eventType = eventHandlerType.BaseType!.GetGenericArguments()[0];

        return eventType.GetProducerName();
    }

    /// <summary>
    /// Extracts the name of the consumer for an event handler
    /// </summary>
    /// <param name="eventHandlerType">Type of the event handler</param>
    /// <returns>Name of the consumer, either custom or automatically generated</returns>
    public static string GetConsumerName(this Type eventHandlerType)
    {
        if (!eventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>)) &&
            !eventHandlerType.IsAssignableTo(typeof(DomainEventHandlerBase<>)))
        {
            throw new InvalidOperationException($"Event handler type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name} or {typeof(DomainEventHandlerBase<>).Name}");
        }
        
        var consumedByAttribute = eventHandlerType
            .GetCustomAttribute<ConsumedByAttribute>(false);

        if (consumedByAttribute is not null)
        {
            return consumedByAttribute.Name;
        }
        
        var namespaceName = eventHandlerType.Namespace!;
        var typeName = eventHandlerType.Name;
        
        return $"{namespaceName}:{typeName}";
    }

    /// <summary>
    /// Extracts the request interval for throttled event handlers
    /// </summary>
    /// <param name="eventHandlerType">Type of the event handler</param>
    /// <returns>Throttled request interval in milliseconds if set, or null</returns>
    public static int? GetThrottledRequestInterval(this Type eventHandlerType)
    {
        if (!eventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>)) &&
            !eventHandlerType.IsAssignableTo(typeof(DomainEventHandlerBase<>)))
        {
            throw new InvalidOperationException($"Event handler type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name} or {typeof(DomainEventHandlerBase<>).Name}");
        }
        
        var throttledAttribute = eventHandlerType
            .GetCustomAttribute<ThrottledAttribute>(false);

        return throttledAttribute?.RequestInterval;
    }
}