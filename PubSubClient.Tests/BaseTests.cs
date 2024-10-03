using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace PubSubClient.Tests;

public abstract class BaseTests<TTest, TConsumer, TPublisher>
    where TTest : class
    where TConsumer : class, IConsumerService
    where TPublisher : class, IPublisherService
{
    private static ILogger<TTest>? logger;

    internal static string? JsonConfiguration;

    internal static IHost? host;

    internal static Consumers? Consumer;

    internal static IPublisherService? Publisher;

    protected virtual int MessageTimeout { get; } = 10000;

    public static async Task BaseInitialize()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConfiguration));

        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Configuration.AddJsonStream(stream);
        hostBuilder.AddPubSubClient<TConsumer, TPublisher>();
        host = hostBuilder.Build();
        logger = host.Services.GetRequiredService<ILogger<TTest>>();
        Consumer = host.Services.GetRequiredService<Consumers>();
        Publisher = host.Services.GetRequiredService<IPublisherService>();
        await host.StartAsync();
        await Task.Delay(1000);
        await host.StopAsync();
    }

    public static async Task BaseCleanup()
    {
        if (host is null) return;
        await host.StopAsync();
        host.Dispose();
    }

    [TestMethod]
    public async Task Publish_Receive_Message_Simple_Type()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        await Publisher.PublishAsync(true, new TestDefinitionThree());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefThreeOutput is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Failed to receive message within timeout.");
        }

        Assert.IsNotNull(Consumer.DefThreeOutput);
        Assert.AreEqual(true, Consumer.DefThreeOutput);
    }

    [TestMethod]
    public async Task Publish_Receive_Message_Simple_Type_Incorrect_Type()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        await Publisher.PublishAsync("Test", new TestDefinitionThree());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefThreeOutput is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            // This is expected as the event will not fire if it cannot convert the type.
            Assert.IsNull(Consumer.DefThreeOutput);
        }
    }

    [TestMethod]
    public async Task Publish_Receive_Message_With_Metadata()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        var message = new TestClass() { Name = "This is a test" };
        await Publisher.PublishAsync(message, new TestDefinition());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefOneOutputObject is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Failed to receive message within timeout.");
        }

        Assert.IsNotNull(Consumer.DefOneOutputObject);
        Assert.IsNotNull(Consumer.DefOneOutput);
        Assert.AreEqual(message.ToString(), Consumer.DefOneOutput?.ToString());
    }

    [TestMethod]
    public async Task Publish_Receive_Message_With_Metadata_Incorrect_Type()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        var message = new TestClassTwo() { Note = "This is a test" };
        await Publisher.PublishAsync(message, new TestDefinition());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefOneOutputObject is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Failed to receive message within timeout.");
        }

        Assert.IsNotNull(Consumer.DefOneOutputObject);
        Assert.IsNotNull(Consumer.DefOneOutput);
        Assert.AreNotEqual(message.ToString(), Consumer.DefOneOutput?.ToString());
    }

    [TestMethod]
    public async Task Publish_Receive_Message_Without_Metadata()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        var message = new TestClassTwo() { Note = "This is a test" };
        await Publisher.PublishAsync(message, new TestDefinitionTwo());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefTwoOutput is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Failed to receive message within timeout.");
        }

        Assert.IsNotNull(Consumer.DefTwoOutput);
        Assert.AreEqual(message.ToString(), Consumer.DefTwoOutput?.ToString());
    }

    [TestMethod]
    public async Task Publish_Receive_Message_Without_Metadata_Incorrect_Type()
    {
        ArgumentNullException.ThrowIfNull(Publisher);
        ArgumentNullException.ThrowIfNull(Consumer);
        Consumer.ResetProperties();

        var message = new TestClass() { Name = "This is a test" };
        await Publisher.PublishAsync(message, new TestDefinitionTwo());

        try
        {
            int t = await Helpers.WaitUntil(() => Consumer.DefTwoOutput is not null, timeout: MessageTimeout);
            logger?.LogInformation("Total response time: {TotalTime}", t);
        }
        catch (TimeoutException)
        {
            Assert.Fail("Failed to receive message within timeout.");
        }

        Assert.IsNotNull(Consumer.DefTwoOutput);
        Assert.AreNotEqual(message.ToString(), Consumer.DefTwoOutput?.ToString());
    }
}