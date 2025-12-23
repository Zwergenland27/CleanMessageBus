using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CleanMessageBus.Abstractions.DependencyInjection;

/// <summary>
/// Configuration for the CleanMessageBus
/// </summary>
public class CleanMessageBusConfiguration(IServiceCollection services)
{
    public readonly IServiceCollection Services = services;
    private readonly HashSet<Type> _integrationEvents = [];
    private readonly HashSet<Type> _integrationEventHandlers = [];
    
    public IReadOnlyCollection<Type> IntegrationEvents => _integrationEvents.ToList().AsReadOnly();
    public IReadOnlyCollection<Type> IntegrationEventHandlers => _integrationEventHandlers.ToList().AsReadOnly();

    public bool MessageBusRegistered = false;

    /// <summary>
    /// Register handlers from assembly <paramref name="assembly"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterHandlersFromAssembly(Assembly assembly)
    {
        RegisterHandlers(assembly);
        return this;
    }

    /// <summary>
    /// Register handlers from list of <paramref name="assemblies"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterHandlersFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterHandlers(assembly);
        }
        return this;
    }
    
    /// <summary>
    /// Register integration events from assembly <paramref name="assembly"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterIntegrationEventsFromAssembly(Assembly assembly)
    {
        RegisterIntegrationEvents(assembly);
        return this;
    }

    /// <summary>
    /// Register integration events from list of <paramref name="assemblies"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterIntegrationEventsFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterIntegrationEvents(assembly);
        }
        
        return this;
    }
    
    private void RegisterHandlers(Assembly assembly)
    {
        Type handlerType = typeof(IRequestHandler<>);
        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces(), (type, @interface) => new { concreteType = type, handlerInterface = @interface })
            .Where(t => t.handlerInterface.IsGenericType && t.handlerInterface.GetGenericTypeDefinition() == handlerType);
        
        foreach (var handler in handlerTypes)
        {
            Services.AddTransient(handler.concreteType);
            _integrationEventHandlers.Add(handler.concreteType);
        }
    }

    private void RegisterIntegrationEvents(Assembly assembly)
    {
        var integrationEvents = assembly
            .GetTypes()
            .Where(p => p.IsAssignableTo(typeof(IIntegrationEvent)));

        foreach (var integrationEvent in integrationEvents)
        {
            _integrationEvents.Add(integrationEvent);
        }
    }
}