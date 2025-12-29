# CleanMessageBus
This package provides an abstraction around message busses for easy integration of events.
Currently the following message brokers are supported:
* RabbitMQ

## Publishing Events
* Event object must implement marker interface ``IIntegrationevent`` for integration events ot ``IDomainEvent`` for domain events
* Inject ``IMessageBus`` to desired service
* Publish event by using ``IMessageBus.PublishAsync(integrationEvent)``
* Automatic creation of producer resource on message bus (for example exchange for rabbitmq)

* Example:
```csharp
public record UserRegisteredEvent(string Username, string Email) : IIntegrationevent;

var integrationEvent = new UserRegisteredEvent("JohnDoe", "john@doe.com");
await messageBus.PublishAsync(integrationevent);
```

### Change producer name
* Use attribute ``[Produces(Name="ProducerName")]`` to set producer name

## Handling Events
* Handler must inherit from ``IntegrationEventHandlerBase<TIntegrationEvent>`` class for integration events or from ``DomainEventHandlerBase<TDomainEvent>`` for domain events
* Generic type specifies domain event type to receive
* Implementation logic must be implemented in the ``Handle`` method
* Registration happens automatically via dependency injection
* Automatic creation of consumer resource on message bus (for example queue for rabbitmq)
* Integration of [CleanDomainValidation](https://github.com/Zwergenland27/CleanDomainValidation) result types

Example:

```csharp
public class UserRegisteredEventHandler : IntegrationEventHandlerBase<UserRegisteredEvent>
{
    public override async Task<CanFail> Handle(UserRegisteredEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"User {@event.Username} with email {@event.Email} has been registered.");
        
        //TODO furhter logic in here
        
        return CanFail.Success;
    }
}
```

### Change consumer name
* Use attribute ``[ConsumedBy(Name="ConsumerName")]`` to set consumer name
* Not allowed for domain events!

### Change producer name
* Use attribute ``[ProducedBy(Name="ProducerName")]`` to set producer name

### Throttle message rate
* Use attribute ``[Throttled(RequestInterval=1000)]`` to set message processing interval
* Message bus waits specified time (in ms) before processing the next event

## Configuration via dependency injection
* Define assemblies containing events and handlers
* Select and configure message broker

Example:
```csharp
builder.Services.AddCleanMessageBus(config => config
    .RegisterIntegrationEventsFromAssembly(Assembly.GetExecutingAssembly())
    .RegisterDomainEventsFromAssemby(Assembly.GetExecutingAssembly())
    .RegisterHandlersFromAssemby(Assembly.GetExecutingAssembly()
    .UseRabbitMq(rabbitConfig => rabbitConfig
        .WithHostname("localhost")
        .WithCredentials("username", "password")
        .UseSsl())));
```