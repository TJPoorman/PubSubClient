namespace PubSubClient;

public interface IConsumerService
{
    Task ReadMessages(CancellationToken token);
}
