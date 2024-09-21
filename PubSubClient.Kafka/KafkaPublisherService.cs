using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.Kafka;

public class KafkaPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<KafkaPublisherService> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());

    public KafkaPublisherService(ILogger<KafkaPublisherService> logger, IConfiguration config)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ProducerConfig>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        _producer = new ProducerBuilder<Null, string>(brokerConfig).Build();

        _logger.LogInformation("Successfully connected publisher to broker.");
    }

    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.GetType()?.FullName ?? string.Empty);

    public async Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey)
    {
        string queueName = $"{exchangeName}-{routingKey}-{_assembly.Value.GetName().Name}";

        await _producer.ProduceAsync(queueName, new Message<Null, string> { Value = JsonSerializer.Serialize(message) });
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        GC.SuppressFinalize(this);
    }
}
