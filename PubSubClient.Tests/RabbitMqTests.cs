using PubSubClient.RabbitMQ;

namespace PubSubClient.Tests;

[TestClass]
public class RabbitMqTests : BaseTests<RabbitMqTests, RabbitMqConsumerService, RabbitMqPublisherService>
{
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        JsonConfiguration = @"{
                ""MessageBroker"": {
                    ""HostName"": ""localhost"",
                    ""DispatchConsumersAsync"": true
                }
            }";

        await BaseInitialize();
    }

    [ClassCleanup]
    public static async Task ClassCleanup() => await BaseCleanup();
}