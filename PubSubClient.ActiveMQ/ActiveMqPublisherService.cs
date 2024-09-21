using Apache.NMS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.ActiveMQ;

public class ActiveMqPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<ActiveMqPublisherService> _logger;
    private readonly IConnection _connection;
    private readonly ISession _session;
    private readonly IMessageProducer _producer;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());

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

    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.GetType()?.FullName ?? string.Empty);

    public async Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey)
    {
        string queueName = $"{exchangeName}-{routingKey}-{_assembly.Value.GetName().Name}";
        ITextMessage m = _producer.CreateTextMessage(JsonSerializer.Serialize(message));
        await _producer.SendAsync(_session.GetQueue(queueName), m);
    }

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
