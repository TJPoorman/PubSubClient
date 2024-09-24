using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PubSubClient.Kafka;
using System.Text;

namespace PubSubClient.Tests
{
    [TestClass]
    public class KafkaTests
    {
        private static IHost? host;
        private static ILogger<KafkaTests>? logger;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            var jsonConfiguration = @"{
                ""MessageBroker"": {
                    ""BootstrapServers"": ""localhost:9092"",
                    ""saslUsername"": ""admin"",
                    ""saslPassword"": ""admin123"",
                    ""SecurityProtocol"": 2,
                    ""SaslMechanism"": 1,
                    ""AutoOffsetReset"": 1
                }
            }";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfiguration));

            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Configuration.AddJsonStream(stream);
            hostBuilder.AddPubSubClient<KafkaConsumerService, KafkaPublisherService>();
            host = hostBuilder.Build();
            logger = host.Services.GetRequiredService<ILogger<KafkaTests>>();
            host.StartAsync();
            await Task.Delay(5000);
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            if (host is null) return;
            await host.StopAsync();
            host.Dispose();
        }

        [TestMethod]
        public async Task Publish_Receive_Message_With_Metadata()
        {
            ArgumentNullException.ThrowIfNull(host);

            var publisher = host?.Services.GetRequiredService<IPublisherService>();
            var consumer = host?.Services.GetRequiredService<Consumers>();
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(consumer);
            consumer.ResetProperties();

            var message = new TestClass() { Name = "This is a test" };
            await publisher.PublishAsync(message, new TestDefinition());

            try
            {
                int t = await Helpers.WaitUntil(() => consumer.DefOneOutputObject is not null, timeout: 20000);
                logger?.LogInformation("Total response time: {TotalTime}", t);
            }
            catch (TimeoutException)
            {
                Assert.Inconclusive("Failed to receive message within timeout.");
            }

            Assert.IsNotNull(consumer.DefOneOutputObject);
            Assert.IsNotNull(consumer.DefOneOutput);
            Assert.AreEqual(message.ToString(), consumer.DefOneOutput?.ToString());
        }

        [TestMethod]
        public async Task Publish_Receive_Message_With_Metadata_Incorrect_Type()
        {
            ArgumentNullException.ThrowIfNull(host);

            var publisher = host?.Services.GetRequiredService<IPublisherService>();
            var consumer = host?.Services.GetRequiredService<Consumers>();
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(consumer);
            consumer.ResetProperties();

            var message = new TestClassTwo() { Note = "This is a test" };
            await publisher.PublishAsync(message, new TestDefinition());

            try
            {
                int t = await Helpers.WaitUntil(() => consumer.DefOneOutputObject is not null, timeout: 20000);
                logger?.LogInformation("Total response time: {TotalTime}", t);
            }
            catch (TimeoutException)
            {
                Assert.Inconclusive("Failed to receive message within timeout.");
            }

            Assert.IsNotNull(consumer.DefOneOutputObject);
            Assert.IsNotNull(consumer.DefOneOutput);
            Assert.AreNotEqual(message.ToString(), consumer.DefOneOutput?.ToString());
        }

        [TestMethod]
        public async Task Publish_Receive_Message_Without_Metadata()
        {
            ArgumentNullException.ThrowIfNull(host);

            var publisher = host?.Services.GetRequiredService<IPublisherService>();
            var consumer = host?.Services.GetRequiredService<Consumers>();
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(consumer);
            consumer.ResetProperties();

            var message = new TestClassTwo() { Note = "This is a test" };
            await publisher.PublishAsync(message, new TestDefinitionTwo());

            try
            {
                int t = await Helpers.WaitUntil(() => consumer.DefTwoOutput is not null, timeout: 20000);
                logger?.LogInformation("Total response time: {TotalTime}", t);
            }
            catch (TimeoutException)
            {
                Assert.Inconclusive("Failed to receive message within timeout.");
            }

            Assert.IsNotNull(consumer.DefTwoOutput);
            Assert.AreEqual(message.ToString(), consumer.DefTwoOutput?.ToString());
        }

        [TestMethod]
        public async Task Publish_Receive_Message_Without_Metadata_Incorrect_Type()
        {
            ArgumentNullException.ThrowIfNull(host);

            var publisher = host?.Services.GetRequiredService<IPublisherService>();
            var consumer = host?.Services.GetRequiredService<Consumers>();
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(consumer);
            consumer.ResetProperties();

            var message = new TestClass() { Name = "This is a test" };
            await publisher.PublishAsync(message, new TestDefinitionTwo());

            try
            {
                int t = await Helpers.WaitUntil(() => consumer.DefTwoOutput is not null, timeout: 20000);
                logger?.LogInformation("Total response time: {TotalTime}", t);
            }
            catch (TimeoutException)
            {
                Assert.Inconclusive("Failed to receive message within timeout.");
            }

            Assert.IsNotNull(consumer.DefTwoOutput);
            Assert.AreNotEqual(message.ToString(), consumer.DefTwoOutput?.ToString());
        }
    }
}