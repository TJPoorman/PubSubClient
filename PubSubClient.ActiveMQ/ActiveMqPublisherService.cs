using Apache.NMS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.ActiveMQ;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses Apache ActiveMQ as the broker for publisher.
/// </remarks>
public class ActiveMqPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<ActiveMqPublisherService> _logger;
    private readonly IConnection _connection;
    private readonly ISession _session;
    private readonly IMessageProducer _producer;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqPublisherService" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddPublisherService{TPublisherService}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <param name="config">The application's configuration settings.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the Message Broker is not provided or is invalid.
    /// </exception>
    public ActiveMqPublisherService(ILogger<ActiveMqPublisherService> logger, IConfiguration config)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ConsumerConfig>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        var factory = new NMSConnectionFactory(brokerConfig.MqServerName);
        _connection = factory.CreateConnection(brokerConfig.MqUserName, brokerConfig.MqPassword);
        _connection.Start();
        _session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        _producer = _session.CreateProducer();

        _logger.LogInformation("Successfully connected publisher to broker.");
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.GetType()?.FullName ?? string.Empty);

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey)
    {
        string queueName = $"{exchangeName}-{routingKey}-{_assembly.Value.GetName().Name}";
        ITextMessage m = _producer.CreateTextMessage(JsonSerializer.Serialize(message));
        await _producer.SendAsync(_session.GetQueue(queueName), m);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _producer?.Close();
        _session?.Close();
        if (_connection is not null && _connection.IsStarted)
        {
            _connection.Stop();
            _connection.Close();
        }
        GC.SuppressFinalize(this);
    }
}
