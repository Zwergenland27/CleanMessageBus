using Microsoft.Extensions.DependencyInjection;

namespace CleanMessageBus.DependencyInjection;

/// <summary>
/// Dependency Injection Features for CleanMessageBus
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCleanMessageBus(this IServiceCollection services, Action<CleanMessageBusConfiguration> configuration)
    {
        var configurationBuilder = new CleanMessageBusConfiguration(services);
        configuration(configurationBuilder);
        
        return services;
    }
}