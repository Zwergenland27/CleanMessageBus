namespace CleanMessageBus;

/// <summary>
/// The configured message bus
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publish integration event <paramref name="integrationEvent"/> to the specified message bus
    /// </summary>
    Task PublishAsync(IIntegrationEvent integrationEvent);
}