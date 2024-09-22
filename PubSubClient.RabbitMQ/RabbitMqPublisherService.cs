using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PubSubClient.RabbitMQ;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses RabbitMQ as the broker for publisher.
/// </remarks>
public class RabbitMqPublisherService : IPublisherService, IDisposable
{
    private readonly ILogger<RabbitMqPublisherService> _logger;
    private readonly IModel _model;
    private readonly IConnection _connection;
    private readonly ConcurrentBag<string> _exchanges = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqPublisherService" class./>
    /// This constructor is intended for use by Dependency Injection and should be used in conjunction with the
    /// <see cref="StartupExtensions.AddPublisherService{TPublisherService}(Microsoft.Extensions.Hosting.IHostApplicationBuilder)"/> method and not called directly.
    /// </summary>
    /// <param name="logger">The logger instance for logging cache operations.</param>
    /// <param name="config">The application's configuration settings.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration of the Message Broker is not provided or is invalid.
    /// </exception>
    public RabbitMqPublisherService(ILogger<RabbitMqPublisherService> logger, IConfiguration config)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ConnectionFactory>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");

        _connection = brokerConfig.CreateConnection();
        _model = _connection.CreateModel();

        _logger.LogInformation("Successfully connected publisher to broker.");
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition) =>
        await PublishAsync(message, serviceDefinition.ExchangeName, serviceDefinition.RoutingKey);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_model.IsOpen) _model.Close();
        if (_connection.IsOpen) _connection.Close();
        GC.SuppressFinalize(this);
    }
}
