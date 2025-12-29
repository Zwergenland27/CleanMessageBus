using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CleanMessageBus.Abstractions.DependencyInjection;

/// <summary>
/// Configuration for the CleanMessageBus
/// </summary>
public class CleanMessageBusConfiguration(IServiceCollection services)
{
    /// <summary>
    /// ServiceCollection for internal usage passed in from dependency injection
    /// </summary>
    public readonly IServiceCollection Services = services;
    private readonly HashSet<Type> _integrationEvents = [];
    private readonly HashSet<Type> _integrationEventHandlers = [];
    private readonly HashSet<Type> _domainEvents = [];
    private readonly HashSet<Type> _domainEventHandlers = [];
    
    /// <summary>
    /// List of registered integration event types
    /// </summary>
    public IReadOnlyCollection<Type> IntegrationEvents => _integrationEvents.ToList().AsReadOnly();
    
    /// <summary>
    /// List of registered integration event handler types
    /// </summary>
    public IReadOnlyCollection<Type> IntegrationEventHandlers => _integrationEventHandlers.ToList().AsReadOnly();
    
    /// <summary>
    /// List of registered domain event types
    /// </summary>
    public IReadOnlyCollection<Type> DomainEvents => _domainEvents.ToList().AsReadOnly();
    
    /// <summary>
    /// List of registered domain event handler types
    /// </summary>
    public IReadOnlyCollection<Type> DomainEventHandlers => _domainEventHandlers.ToList().AsReadOnly();

    /// <summary>
    /// Indicates whether one message bus is registered
    /// </summary>
    public bool MessageBusRegistered = false;

    /// <summary>
    /// Register handlers from assembly <paramref name="assembly"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterHandlersFromAssembly(Assembly assembly)
    {
        RegisterIntegrationEventHandlers(assembly);
        RegisterDomainEventHandlers(assembly);
        return this;
    }

    /// <summary>
    /// Register handlers from list of <paramref name="assemblies"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterHandlersFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterIntegrationEventHandlers(assembly);
            RegisterDomainEventHandlers(assembly);
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
    
    /// <summary>
    /// Register domain events from assembly <paramref name="assembly"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterDomainEventsFromAssembly(Assembly assembly)
    {
        RegisterDomainevents(assembly);
        return this;
    }

    /// <summary>
    /// Register domain events from list of <paramref name="assemblies"/>
    /// </summary>
    public CleanMessageBusConfiguration RegisterDomainEventsFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterDomainevents(assembly);
        }
        
        return this;
    }
    
    private void RegisterIntegrationEventHandlers(Assembly assembly)
    {
        Type handlerType = typeof(IntegrationEventHandlerBase<>);
        var handlerTypes = assembly.GetTypes()
            .Where(type =>
                type.BaseType is { IsGenericType: true } && 
                type.BaseType.GetGenericTypeDefinition() == handlerType);
        
        foreach (var handler in handlerTypes)
        {
            Services.AddTransient(handler);
            _integrationEventHandlers.Add(handler);
        }
    }
    
    private void RegisterDomainEventHandlers(Assembly assembly)
    {
        Type handlerType = typeof(DomainEventHandlerBase<>);
        var handlerTypes = assembly.GetTypes()
            .Where(type =>
                type.BaseType is { IsGenericType: true } && 
                type.BaseType.GetGenericTypeDefinition() == handlerType);
        
        foreach (var handler in handlerTypes)
        {
            Services.AddTransient(handler);
            _domainEventHandlers.Add(handler);
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

    private void RegisterDomainevents(Assembly assembly)
    {
        var domainEvents = assembly
            .GetTypes()
            .Where(p => p.IsAssignableTo(typeof(IDomainEvent)));

        foreach (var domainevent in domainEvents)
        {
            _domainEvents.Add(domainevent);
        }
    }
}