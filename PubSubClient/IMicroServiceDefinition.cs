namespace PubSubClient;

/// <summary>
/// Interface used by the framework to define how the messages are routed from publishers to consumers.
/// </summary>
public interface IMicroServiceDefinition
{
    /// <summary>
    /// The name of the exchange if the service uses it; otherwise used a prefix in flat topic systems.
    /// </summary>
    string ExchangeName { get; }

    /// <summary>
    /// The unique <see cref="string"/> used to define where messages are routed in the broker.
    /// </summary>
    string RoutingKey { get; }

    /// <summary>
    /// Returns a <see cref="string"/> representation of the definition.
    /// </summary>
    string ToString();
}
