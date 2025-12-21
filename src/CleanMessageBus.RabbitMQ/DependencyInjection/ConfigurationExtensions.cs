using CleanMessageBus.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanMessageBus.RabbitMQ.DependencyInjection;

/// <summary>
/// Extensions of <see cref="CleanMessageBusConfiguration"/> for Adding RabbitMQ as message bus
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Use rabbitmq as message bus
    /// </summary>
    /// <param name="messageBusConfiguration">configuration for message bus</param>
    /// <param name="configuration">configuration for rabbitmq</param>
    public static CleanMessageBusConfiguration UseRabbitMq(this CleanMessageBusConfiguration messageBusConfiguration, Action<RabbitMqConfiguration> configuration)
    {
        if(messageBusConfiguration.MessageBusRegistered) throw new InvalidOperationException("A service bus has already been registered.");
        messageBusConfiguration.MessageBusRegistered = true;
        
        var configurationBuilder = new RabbitMqConfiguration();
        configuration(configurationBuilder);
        
        messageBusConfiguration.Services.AddSingleton<RabbitMqBus>(serviceProvider => new RabbitMqBus(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<ILogger<RabbitMqBus>>(),
            messageBusConfiguration.IntegrationEvents,
            messageBusConfiguration.IntegrationEventHandlers,
            configurationBuilder.Host,
            configurationBuilder.Username,
            configurationBuilder.Password,
            configurationBuilder.SslOptions));
        
        messageBusConfiguration.Services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<RabbitMqBus>());
        return messageBusConfiguration;
    }
}