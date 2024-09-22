using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace PubSubClient;

/// <summary>
/// Background service used to create consumers and method handlers for receiving messages.
/// </summary>
public class AsyncConsumerBackgroundService : BackgroundService
{
    private readonly ConcurrentBag<IConsumerService> _consumerService = new();

    /// <summary>
    /// Adds an instance of <see cref="IConsumerService"/> to the collection of services.
    /// </summary>
    /// <param name="consumerService">The instance of <see cref="IConsumerService"/> to be added.</param>
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
