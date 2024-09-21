using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace PubSubClient;

public class AsyncConsumerBackgroundService : BackgroundService
{
    private readonly ConcurrentBag<IConsumerService> _consumerService = new();

    public void AddConsumer(IConsumerService consumerService) => _consumerService.Add(consumerService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> tasks = new();
        foreach (IConsumerService service in _consumerService)
        {
            tasks.Add(service.ReadMessages(stoppingToken));
        }
        await Task.WhenAll(tasks);
    }
}
