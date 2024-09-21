using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PubSubClient.Kafka;

public class KafkaConsumerService : IConsumerService, IDisposable
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly Delegate _action;
    private readonly static Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    private readonly bool _twoParam;

    public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConfiguration config, IMicroServiceDefinition definition, Delegate action)
    {
        _logger = logger;

        var brokerConfig = config.GetSection("MessageBroker").Get<ConsumerConfig>()
            ?? throw new InvalidOperationException("Configuration section 'MessageBroker' is missing.");
        brokerConfig.GroupId = definition.ExchangeName;

        string queueName = $"{definition.ExchangeName}-{definition.GetType()?.FullName}-{_assembly.Value.GetName().Name}";
        EnsureTopicExistsAsync(brokerConfig, queueName);

        _consumer = new ConsumerBuilder<Ignore, string>(brokerConfig).Build();
        _consumer.Subscribe(queueName);
        _action = action;
        _twoParam = action.Method.GetParameters().Length > 1;

        _logger.LogDebug("Declared/Bound queue: {Exchange} | {QueueName} | {RoutingKey}", definition.ExchangeName, queueName, definition.RoutingKey);
    }

    public async Task ReadMessages(CancellationToken token) => await Task.Run(() => StartListenerLoop(token), token);

    void EnsureTopicExistsAsync(ConsumerConfig config, string topicName, int numPartitions = 1, short replicationFactor = 1)
    {
        using var adminClient = new AdminClientBuilder(config).Build();
        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            bool topicExists = metadata.Topics.Exists(t => t.Topic == topicName && t.Error.Code == ErrorCode.NoError);
            if (topicExists) return;

            var topicSpecification = new TopicSpecification
            {
                Name = topicName,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            };

            adminClient.CreateTopicsAsync(new List<TopicSpecification>() { topicSpecification }).Wait();
        }
        catch (CreateTopicsException e)
        {
            foreach (var result in e.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists) continue;
                _logger.LogError(e, "An error occurred creating the topic: {Reason}", result.Error.Reason);
            }
        }
    }

    private async Task StartListenerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<Ignore, string> cr = _consumer.Consume(token);
                var obj = JsonSerializer.Deserialize(cr.Message.Value, _action.Method.GetParameters()[_twoParam ? 1 : 0].ParameterType);
                if (obj is not null)
                {
                    if (!_twoParam)
                    {
                        _action.DynamicInvoke(obj);
                    }
                    else
                    {
                        _action.DynamicInvoke(cr, obj);
                    }
                }
                await Task.FromResult(0);

                _logger.LogDebug("Received/Acked message: {Partition} - {Offset}", cr.Partition.Value, cr.Offset.Value);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                _logger.LogError(e, "Consume error: {Reason}", e.Error.Reason);

                if (e.Error.IsFatal)  break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error");
                break;
            }
        }
    }

    public void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
