namespace PubSubClient;

/// <summary>
/// Interface for publishing services to send messages to the broker.
/// </summary>
public interface IPublisherService
{
    /// <summary>
    /// Publishes a message to the broker with the specified <see cref="IMicroServiceDefinition"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being published.</typeparam>
    /// <param name="message">The message to be published.</param>
    /// <param name="serviceDefinition">The defintion to be used for publishing the message.</param>
    Task PublishAsync<TMessage>(TMessage message, IMicroServiceDefinition serviceDefinition);

    /// <summary>
    /// Publishes a message to the broker with the specified <paramref name="exchangeName"/> and <paramref name="routingKey"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being published.</typeparam>
    /// <param name="message">The message to be published.</param>
    /// <param name="exchangeName">A <see cref="string"/> designating the exchange to use for publishing.</param>
    /// <param name="routingKey">A <see cref="string"/> desginating the routing key to use for publishing.</param>
    Task PublishAsync<TMessage>(TMessage message, string exchangeName, string routingKey);
}
