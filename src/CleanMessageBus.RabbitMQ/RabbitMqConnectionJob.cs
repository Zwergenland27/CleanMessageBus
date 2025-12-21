using Microsoft.Extensions.Hosting;

namespace CleanMessageBus.RabbitMQ;

internal class RabbitMqConnectionJob(RabbitMqBus rabbitMqBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await rabbitMqBus.ConnectAsync();
    }
}