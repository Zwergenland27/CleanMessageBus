using System.Reflection;
using System.Text;
using System.Text.Json;
using CleanDomainValidation.Domain;
using CleanMessageBus.Abstractions;
using CleanMessageBus.Abstractions.HandlerAttributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CleanMessageBus.RabbitMQ;

internal class RabbitMqBus(
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqBus> logger,
    IReadOnlyCollection<Type> integrationEvents,
    IReadOnlyCollection<Type> integrationEventHandlers,
    string hostname,
    string username,
    string password,
    SslOption sslOption) : IMessageBus, IDisposable, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _normalChannel;
    private IChannel? _scheduledChannel;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed = false;
    
    public async Task PublishAsync(IIntegrationEvent integrationEvent)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");
        
        var exchangeName = integrationEvent.GetType().FullName!;
        var body = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        
        await _normalChannel.BasicPublishAsync(exchange: exchangeName, routingKey: string.Empty, body: Encoding.UTF8.GetBytes(body));
        logger.LogInformation("Published event {EventName} to exchange {ExchangeName}", integrationEvent.GetType().Name, exchangeName);
    }

    private async Task RegisterIntegrationEvent(Type integrationEventType)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");

        var eventName = integrationEventType.FullName!;

        await _normalChannel.ExchangeDeclareAsync(exchange: eventName, type: ExchangeType.Fanout, durable: true);
        logger.LogDebug("Declared exchange {ExchangeName}", eventName);
    }
    

    private async Task RegisterHandler(Type integrationEventHandlerType)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");
        
        var integrationEventType = integrationEventHandlerType.BaseType!.GetGenericArguments()[0];
        
        var defaultProducerName = integrationEventType.FullName!;
        var defaultQueueName = integrationEventHandlerType.FullName!;

        var producedByAttribute = integrationEventHandlerType
            .GetCustomAttribute<ProducedByAttribute>(false);

        var consumedByAttribute = integrationEventHandlerType
            .GetCustomAttribute<ConsumedByAttribute>(false);
        
        var throttledAttribute = integrationEventHandlerType
            .GetCustomAttribute<ThrottledAttribute>(false);
        
        var exchangeName = producedByAttribute?.Name ?? defaultProducerName;
        var queueName = consumedByAttribute?.Name ?? defaultQueueName;
        
        await _normalChannel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        logger.LogDebug("Declared queue {QueueName}", queueName);
        
        await _normalChannel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: string.Empty);
        logger.LogDebug("Bound queue {QueueName} to exchange {ExchangeName}", queueName, exchangeName);

        var channel = _normalChannel;
        if (throttledAttribute is not null)
        {
            channel = _scheduledChannel!;
            logger.LogDebug("Use throttled channel for queue {QueueName}", queueName);
        }
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var content =  Encoding.UTF8.GetString(body);
            
                var integrationEvent = JsonSerializer.Deserialize(content, integrationEventType);
                if(integrationEvent is null) throw new InvalidDataException($"Integration event '{defaultProducerName}' cannot be deserialized.");

                await using var scope = scopeFactory.CreateAsyncScope();
                dynamic handler = scope.ServiceProvider.GetRequiredService(integrationEventHandlerType);
            
                logger.LogInformation("Received event {EventName}", integrationEvent.GetType().Name);
                CanFail result = await handler.Handle((dynamic)integrationEvent, _cancellationTokenSource.Token);

                if (throttledAttribute is not null)
                {
                    await Task.Delay(throttledAttribute.RequestInterval);
                }

                if (result.HasFailed)
                {
                    logger.LogWarning("Handling event {EventName} failed", integrationEvent.GetType().Name);
                    //TODO: error handling
                }
                await channel.BasicAckAsync(ea.DeliveryTag, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occured while handling event {EventName}", integrationEventType.Name);
            }
        };
        
        await channel.BasicConsumeAsync(queue: queueName, consumer: consumer, autoAck: false);
        logger.LogDebug("Registered consumer for queue  {QueueName}", queueName);
    }

    public async Task ConnectAsync()
    {
        if (_connection is not null && _connection.IsOpen) return;

        var factory = new ConnectionFactory()
        {
            HostName = hostname,
            UserName = username,
            Password = password,
            Ssl = sslOption
        };
        
        _connection = await factory.CreateConnectionAsync();
        _normalChannel = await _connection.CreateChannelAsync();

        _scheduledChannel = await _connection.CreateChannelAsync();
        await _scheduledChannel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);
        
        logger.LogInformation("Connection to Broker at {Hostname} established", hostname);

        foreach (var integrationEvent in integrationEvents)
        {
            await RegisterIntegrationEvent(integrationEvent);
        }

        foreach (var integrationEventHandler in integrationEventHandlers)
        {
            await  RegisterHandler(integrationEventHandler);
        }
    }

    public void Dispose()
    {
        if(_disposed) return;
        _cancellationTokenSource.Cancel();
        _connection?.Dispose();
        _normalChannel?.Dispose();
        _cancellationTokenSource.Dispose();
        logger.LogInformation("Connection to Broker at {Hostname} closed", hostname);
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if(_disposed) return;
        await _cancellationTokenSource.CancelAsync();
        if (_connection != null) await _connection.DisposeAsync();
        if (_normalChannel != null) await _normalChannel.DisposeAsync();
        _cancellationTokenSource.Dispose();
        logger.LogInformation("Connection to Broker at {Hostname} closed", hostname);
        _disposed = true;
    }
}