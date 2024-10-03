using Apache.NMS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace PubSubClient.ActiveMQ;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses Apache ActiveMQ as the broker for consumer.
/// </remarks>
public class ActiveMqConsumerService : IConsumerService, IDisposable
{
    private readonly ILogger<ActiveMqConsumerService> _logger;
    private readonly IConnection _connection;
    private readonly ISession _session;
    private readonly IMessageConsumer _consumer;
    private readonly Delegate _action;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    private readonly bool _twoParam;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqConsumerService" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddConsumerServices{TConsumerService}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <param name="config">The application's configuration settings.</param>
    /// <param name="definition">The <see cref="IMicroServiceDefinition"/> to receive messages from.</param>
    /// <param name="action">The <see cref="Delegate"/> used to specify the method to execute when a message is received.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the Message Broker is not provided or is invalid.
    /// </exception>
    public ActiveMqConsumerService(ILogger<ActiveMqConsumerService> logger, IConfiguration config, IMicroServiceDefinition definition, Delegate action)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ConsumerConfig>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        string queueName = $"{definition.ExchangeName}-{definition.GetType()?.FullName}-{_assembly.Value.GetName().Name}";

        var factory = new NMSConnectionFactory(brokerConfig.MqServerName);
        _connection = factory.CreateConnection(brokerConfig.MqUserName, brokerConfig.MqPassword);
        _connection.Start();
        _session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        _consumer = _session.CreateConsumer(_session.GetQueue(queueName));
        _action = action;
        _twoParam = action.Method.GetParameters().Length > 1;

        _logger.LogDebug("Declared/Bound queue: {Exchange} | {QueueName} | {RoutingKey}", definition.ExchangeName, queueName, definition.RoutingKey);
    }

    /// <inheritdoc/>
    public async Task ReadMessages(CancellationToken token)
    {
        _consumer.Listener += async (m) =>
        {
            var body = m.Body<string>();
            object? obj;
            try
            {
                obj = JsonSerializer.Deserialize(body, _action.Method.GetParameters()[_twoParam ? 1 : 0].ParameterType);
            }
            catch
            {
                obj = null;
            }

            if (obj is not null)
            {
                if (!_twoParam)
                {
                    _action.DynamicInvoke(obj);
                }
                else
                {
                    _action.DynamicInvoke(m, obj);
                }
            }
            await Task.CompletedTask;

            _logger.LogDebug("Received/Acked message: {CorrelationId}", m.NMSCorrelationID);
        };
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _consumer?.Close();
        _session?.Close();
        if (_connection is not null && _connection.IsStarted)
        {
            _connection.Stop();
            _connection.Close();
        }
        GC.SuppressFinalize(this);
    }
}
