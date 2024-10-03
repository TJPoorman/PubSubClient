using PubSubClient.Kafka;

namespace PubSubClient.Tests;

[TestClass]
public class KafkaTests : BaseTests<KafkaTests, KafkaConsumerService, KafkaPublisherService>
{
    protected override int MessageTimeout => 20000;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        JsonConfiguration = @"{
                ""MessageBroker"": {
                    ""BootstrapServers"": ""localhost:9092"",
                    ""saslUsername"": ""admin"",
                    ""saslPassword"": ""admin123"",
                    ""SecurityProtocol"": 2,
                    ""SaslMechanism"": 1,
                    ""AutoOffsetReset"": 1
                }
            }";

        await BaseInitialize();
    }

    [ClassCleanup]
    public static async Task ClassCleanup() => await BaseCleanup();
}