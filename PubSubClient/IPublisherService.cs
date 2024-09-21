namespace PubSubClient;

public interface IPublisherService
{
    Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition);

    Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey);
}
