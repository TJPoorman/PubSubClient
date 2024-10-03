using PubSubClient.ActiveMQ;

namespace PubSubClient.Tests;

[TestClass]
public class ActivemqTests : BaseTests<ActivemqTests, ActiveMqConsumerService, ActiveMqPublisherService>
{
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        JsonConfiguration = @"{
                ""MessageBroker"": {
                    ""MqServerName"": ""activemq:tcp://localhost:61616"",
                    ""MqUserName"": ""artemis"",
                    ""MqPassword"": ""artemis""
                }
            }";

        await BaseInitialize();
    }

    [ClassCleanup]
    public static async Task ClassCleanup() => await BaseCleanup();
}