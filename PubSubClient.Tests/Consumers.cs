using PubSubClient.Attributes;

namespace PubSubClient.Tests;

internal class Consumers
{
    internal object? DefOneOutputObject { get; private set; } = null;
    internal TestClass? DefOneOutput { get; private set; } = null;
    internal TestClassTwo? DefTwoOutput { get; private set; } = null;

    internal void ResetProperties()
    {
        DefOneOutputObject = null;
        DefOneOutput = null;
        DefTwoOutput = null;
    }


    [ConsumerMethod(typeof(TestDefinition))]
    internal async Task RunDefOne(object o, TestClass c)
    {
        await Task.FromResult(0);
        DefOneOutputObject = o;
        DefOneOutput = c;
    }

    [ConsumerMethod(typeof(TestDefinitionTwo))]
    internal async Task RunDefTwo(TestClassTwo c)
    {
        await Task.FromResult(0);
        DefTwoOutput = c;
    }
}
