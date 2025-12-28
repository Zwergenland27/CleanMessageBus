using System.Reflection;
using CleanMessageBus.Abstractions.Attributes;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Extension methods to centralize naming
/// </summary>
public static class NamingExtensions
{
    /// <summary>
    /// Extracts the producer name from integration event
    /// </summary>
    /// <param name="integrationEventType">Type of the integration event</param>
    /// <returns>Name of the producer, either custom or automatically generated</returns>
    public static string GetProducerName(this Type integrationEventType)
    {
        if(!integrationEventType.IsAssignableTo(typeof(IIntegrationEvent))) throw new InvalidOperationException($"Integration event type must be assignable to {nameof(IIntegrationEvent)}");
        var producesAttribute = integrationEventType
            .GetCustomAttribute<ProducesAttribute>(false);

        if (producesAttribute is not null)
        {
            return producesAttribute.Name;
        }
       
        var namespaceName = integrationEventType.Namespace!;
        var typeName = integrationEventType.Name;
        
        return $"{namespaceName}:{typeName}";
    }

    /// <summary>
    /// Extracts the name of the producing integration event for an event handler
    /// </summary>
    /// <param name="integrationEventHandlerType">Type of the integration event handler</param>
    /// <returns>Name of the producer, either custom or automatically generated</returns>
    public static string GetProducedByName(this Type integrationEventHandlerType)
    {
        if(!integrationEventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>))) throw new InvalidOperationException($"Integration event type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name}");
        
        var producedByAttribute = integrationEventHandlerType
            .GetCustomAttribute<ProducedByAttribute>(false);

        if (producedByAttribute is not null)
        {
            return producedByAttribute.Name;
        }
        
        var integrationEventType = integrationEventHandlerType.BaseType!.GetGenericArguments()[0];

        return integrationEventType.GetProducerName();
    }

    /// <summary>
    /// Extracts the name of the consumer for an event handler
    /// </summary>
    /// <param name="integrationEventHandlerType">Type of the integration event handler</param>
    /// <returns>Name of the consumer, either custom or automatically generated</returns>
    public static string GetConsumerName(this Type integrationEventHandlerType)
    {
        if(!integrationEventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>))) throw new InvalidOperationException($"Integration event type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name}");
        
        var consumedByAttribute = integrationEventHandlerType
            .GetCustomAttribute<ConsumedByAttribute>(false);

        if (consumedByAttribute is not null)
        {
            return consumedByAttribute.Name;
        }
        
        var namespaceName = integrationEventHandlerType.Namespace!;
        var typeName = integrationEventHandlerType.Name;
        
        return $"{namespaceName}:{typeName}";
    }

    /// <summary>
    /// Extracts the request interval for throttled event handlers
    /// </summary>
    /// <param name="integrationEventHandlerType">Type of the integration event handler</param>
    /// <returns>Throttled request interval in milliseconds if set, or null</returns>
    public static int? GetThrottledRequestInterval(this Type integrationEventHandlerType)
    {
        if(!integrationEventHandlerType.IsAssignableTo(typeof(IntegrationEventHandlerBase<>))) throw new InvalidOperationException($"Integration event type must be assignable to {typeof(IntegrationEventHandlerBase<>).Name}");

        var throttledAttribute = integrationEventHandlerType
            .GetCustomAttribute<ThrottledAttribute>(false);

        return throttledAttribute?.RequestInterval;
    }
}