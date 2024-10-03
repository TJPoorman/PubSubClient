using PubSubClient.Attributes;

namespace PubSubClient.Tests;

internal class Consumers
{
    internal object? DefOneOutputObject { get; private set; } = null;
    internal TestClass? DefOneOutput { get; private set; } = null;
    internal TestClassTwo? DefTwoOutput { get; private set; } = null;
    internal bool? DefThreeOutput { get; private set; } = null;

    internal void ResetProperties()
    {
        DefOneOutputObject = null;
        DefOneOutput = null;
        DefTwoOutput = null;
        DefThreeOutput = null;
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

    [ConsumerMethod(typeof(TestDefinitionThree))]
    internal async Task RunDefThree(bool b)
    {
        await Task.FromResult(0);
        DefThreeOutput = b;
    }
}
