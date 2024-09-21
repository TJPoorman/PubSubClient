namespace PubSubClient;

public interface IMicroServiceDefinition
{
    string ExchangeName { get; }
    string RoutingKey { get; }

    string ToString();
}
