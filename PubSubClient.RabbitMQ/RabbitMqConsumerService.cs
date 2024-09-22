using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.RabbitMQ;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses RabbitMQ as the broker for consumer.
/// </remarks>
public class RabbitMqConsumerService : IConsumerService, IDisposable
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IModel _model;
    private readonly IConnection _connection;
    private readonly Delegate _action;
    private readonly string _queueName;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    private readonly bool _twoParam;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConsumerService" class./>
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
    public RabbitMqConsumerService(ILogger<RabbitMqConsumerService> logger, IConfiguration config, IMicroServiceDefinition definition, Delegate action)
    {
        _logger = logger;

        ConnectionFactory? brokerConfig = config.GetRequiredSection("MessageBroker").Get<ConnectionFactory>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        _queueName = $"{definition.GetType()?.FullName}-{_assembly.Value.GetName().Name}";
        _connection = brokerConfig.CreateConnection();
        _model = _connection.CreateModel();
        _model.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        _model.ExchangeDeclare(definition.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        _model.QueueBind(_queueName, definition.ExchangeName, definition.RoutingKey);
        _action = action;
        _twoParam = action.Method.GetParameters().Length > 1;

        _logger.LogDebug("Declared/Bound queue: {Exchange} | {QueueName} | {RoutingKey}", definition.ExchangeName, _queueName, definition.RoutingKey);
    }

    /// <inheritdoc/>
    public async Task ReadMessages(CancellationToken token)
    {
        var consumer = new AsyncEventingBasicConsumer(_model);
        consumer.Received += async (ch, ea) =>
        {
            var body = ea.Body.ToArray();
            var text = System.Text.Encoding.UTF8.GetString(body);
            var obj = JsonSerializer.Deserialize(text, _action.Method.GetParameters()[_twoParam ? 1 : 0].ParameterType);
            if (obj is not null)
            {
                if (!_twoParam)
                {
                    _action.DynamicInvoke(obj);
                }
                else
                {
                    _action.DynamicInvoke(ea, obj);
                }
            }
            await Task.CompletedTask;
            _model.BasicAck(ea.DeliveryTag, false);

            _logger.LogDebug("Received/Acked message: {DeliveryTag}", ea.DeliveryTag);
        };
        _model.BasicConsume(_queueName, false, consumer);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_model.IsOpen) _model.Close();
        if (_connection.IsOpen) _connection.Close();
        GC.SuppressFinalize(this);
    }
}
