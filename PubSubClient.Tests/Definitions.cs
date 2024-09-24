using System.Text.Json;

namespace PubSubClient.Tests;

internal class TestExchangeBaseDef : MicroServiceDefinitionBase { public override string ExchangeName => "Test"; }
internal class TestDefinition : TestExchangeBaseDef { }
internal class TestDefinitionTwo : TestExchangeBaseDef { }

internal class TestClass
{
    public string? Name { get; set; }
    public override string ToString() => JsonSerializer.Serialize(this);
}

internal class TestClassTwo
{
    public string? Note { get; set; }
    public override string ToString() => JsonSerializer.Serialize(this);
}
