using System.Text;
using System.Text.Json;
using CleanDomainValidation.Domain;
using CleanMessageBus.Abstractions;
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
    IReadOnlyCollection<Type> domainEvents,
    IReadOnlyCollection<Type> domainEventHandlers,
    string hostname,
    string username,
    string password,
    SslOption sslOption) : IMessageBus, IDisposable, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _normalChannel;
    private List<IChannel> _scheduledChannels = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed;
    
    public async Task PublishAsync(IIntegrationEvent integrationEvent)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");
        
        var exchangeName = integrationEvent.GetType().GetProducerName();
        var body = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        
        await _normalChannel.BasicPublishAsync(exchange: exchangeName, routingKey: string.Empty, body: Encoding.UTF8.GetBytes(body));
        logger.LogInformation("Published event {EventName} to exchange {ExchangeName}", integrationEvent.GetType().Name, exchangeName);
    }
    
    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");
        
        var exchangeName = domainEvent.GetType().GetProducerName();
        var body = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        
        await _normalChannel.BasicPublishAsync(exchange: exchangeName, routingKey: string.Empty, body: Encoding.UTF8.GetBytes(body));
        logger.LogInformation("Published event {EventName} to exchange {ExchangeName}", domainEvent.GetType().Name, exchangeName);
    }

    private async Task RegisterEventAsync(Type eventType)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");

        var exchangeName = eventType.GetProducerName();

        await _normalChannel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);
        logger.LogDebug("Declared exchange {ExchangeName}", exchangeName);
    }

    private async Task RegisterHandlerAsync(Type eventHandlerType)
    {
        if(_normalChannel is null) throw new InvalidOperationException("RabbitMQBus has not been initialized.");

        var exchangeName = eventHandlerType.GetProducedByName();
        var queueName = eventHandlerType.GetConsumerName();
        var throttledRequestInterval = eventHandlerType.GetThrottledRequestInterval();
        
        await _normalChannel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        logger.LogDebug("Declared queue {QueueName}", queueName);
        
        await _normalChannel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: string.Empty);
        logger.LogDebug("Bound queue {QueueName} to exchange {ExchangeName}", queueName, exchangeName);

        var channel = _normalChannel;
        if (throttledRequestInterval is not null)
        {
            channel = await _connection!.CreateChannelAsync();
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);
            _scheduledChannels.Add(channel);
            logger.LogDebug("Use throttled channel for queue {QueueName}", queueName);
        }
        
                
        var eventType = eventHandlerType.BaseType!.GetGenericArguments()[0];
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var content =  Encoding.UTF8.GetString(body);
            
                var @event = JsonSerializer.Deserialize(content, eventType);
                if(@event is null) throw new InvalidDataException($"Event '{eventType.Name}' cannot be deserialized.");

                await using var scope = scopeFactory.CreateAsyncScope();
                dynamic handler = scope.ServiceProvider.GetRequiredService(eventHandlerType);
            
                logger.LogInformation("Received event {EventName}", @event.GetType().Name);
                CanFail result = await handler.Handle((dynamic)@event, _cancellationTokenSource.Token);

                if (throttledRequestInterval is not null)
                {
                    await Task.Delay(throttledRequestInterval.Value);
                }

                if (result.HasFailed)
                {
                    logger.LogWarning("Handling event {EventName} failed", @event.GetType().Name);
                    //TODO: error handling
                }
                await channel.BasicAckAsync(ea.DeliveryTag, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occured while handling event {EventName}", eventType.Name);
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
        
        logger.LogInformation("Connection to Broker at {Hostname} established", hostname);

        foreach (var integrationEvent in integrationEvents)
        {
            await RegisterEventAsync(integrationEvent);
        }

        foreach (var integrationEventHandler in integrationEventHandlers)
        {
            await  RegisterHandlerAsync(integrationEventHandler);
        }

        foreach (var domainEvent in domainEvents)
        {
            await RegisterEventAsync(domainEvent);
        }
        
        foreach (var domainEventHandler in domainEventHandlers)
        {
            await  RegisterHandlerAsync(domainEventHandler);
        }
    }

    public void Dispose()
    {
        if(_disposed) return;
        _cancellationTokenSource.Cancel();
        _connection?.Dispose();
        _normalChannel?.Dispose();
        foreach (var channel in _scheduledChannels)
        {
            channel.Dispose();
        }
        _scheduledChannels.Clear();
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
        foreach (var channel in _scheduledChannels)
        {
            await channel.DisposeAsync();
        }
        _scheduledChannels.Clear();
        _cancellationTokenSource.Dispose();
        logger.LogInformation("Connection to Broker at {Hostname} closed", hostname);
        _disposed = true;
    }
}