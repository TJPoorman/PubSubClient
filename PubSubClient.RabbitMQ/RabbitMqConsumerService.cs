using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.RabbitMQ;

public class RabbitMqConsumerService : IConsumerService, IDisposable
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IModel _model;
    private readonly IConnection _connection;
    private readonly Delegate _action;
    private readonly string _queueName;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    private readonly bool _twoParam;

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

    public void Dispose()
    {
        if (_model.IsOpen) _model.Close();
        if (_connection.IsOpen) _connection.Close();
        GC.SuppressFinalize(this);
    }
}
