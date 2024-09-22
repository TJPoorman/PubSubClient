namespace PubSubClient;

/// <summary>
/// Interface for consuming services to read messages from the broker.
/// </summary>
public interface IConsumerService
{
    /// <summary>
    /// This method is used to read messages from the broker.
    /// </summary>
    /// <param name="token"><see cref="CancellationToken"/> used to cancel the consumption of messages</param>
    Task ReadMessages(CancellationToken token);
}
