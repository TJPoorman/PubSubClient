using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PubSubClient.RabbitMQ;

public class RabbitMqPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<RabbitMqPublisherService> _logger;
    private readonly IModel _model;
    private readonly IConnection _connection;
    private readonly ConcurrentBag<string> _exchanges = new();

    public RabbitMqPublisherService(ILogger<RabbitMqPublisherService> logger, IConfiguration config)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ConnectionFactory>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        _connection = brokerConfig.CreateConnection();
        _model = _connection.CreateModel();

        _logger.LogInformation("Successfully connected publisher to broker.");
    }

    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.RoutingKey);

    public async Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey)
    {
        if (!_exchanges.Any(a => a.Equals(exchangeName)))
        {
            _model.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            _exchanges.Add(exchangeName);
        }

        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _model.BasicPublish(exchangeName, routingKey, null, body);

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_model.IsOpen) _model.Close();
        if (_connection.IsOpen) _connection.Close();
        GC.SuppressFinalize(this);
    }
}
