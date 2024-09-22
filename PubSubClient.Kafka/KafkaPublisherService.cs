using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.Kafka;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses Confluent Kafka as the broker for publisher.
/// </remarks>
public class KafkaPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<KafkaPublisherService> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublisherService" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddPublisherService{TPublisherService}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <param name="config">The application's configuration settings.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the Message Broker is not provided or is invalid.
    /// </exception>
    public KafkaPublisherService(ILogger<KafkaPublisherService> logger, IConfiguration config)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ProducerConfig>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        _producer = new ProducerBuilder<Null, string>(brokerConfig).Build();

        _logger.LogInformation("Successfully connected publisher to broker.");
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.GetType()?.FullName ?? string.Empty);

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey)
    {
        string queueName = $"{exchangeName}-{routingKey}-{_assembly.Value.GetName().Name}";

        await _producer.ProduceAsync(queueName, new Message<Null, string> { Value = JsonSerializer.Serialize(message) });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        GC.SuppressFinalize(this);
    }
}
