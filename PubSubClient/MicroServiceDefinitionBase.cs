using System.Reflection;

namespace PubSubClient;

/// <summary>
/// An abstract base <see cref="IMicroServiceDefinition"/> for all micro service definitions.
/// </summary>
public abstract class MicroServiceDefinitionBase : IMicroServiceDefinition
{
    private readonly object _routingKeyLock = new();
    private readonly Lazy<Assembly> _assembly = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    private ServicePool _pool;
    private string? _routingKey;

    public abstract string ExchangeName { get; }

    public ServicePool Pool
    {
        get => _pool;
        set
        {
            if (_pool != value)
            {
                lock (_routingKeyLock)
                {
                    _routingKey = null; // reset routing key so it will be regenerated using the updated _pool value.
                    _pool = value;
                }
            }
        }
    }

    public string QueueName => $"{GetType()?.FullName}-{_assembly.Value.GetName().Name}";

    public virtual string RoutingKey
    {
        get
        {
            if (_routingKey is not null) return _routingKey;

            lock (_routingKeyLock)
            {
                if (_routingKey is not null) return _routingKey;

                _routingKey = GetType()?.FullName?.Replace(GetType()?.Namespace ?? string.Empty, string.Empty) ?? string.Empty;

                while (_routingKey.StartsWith('.'))
                {
                    _routingKey = _routingKey[1..];
                }

                if (Pool != ServicePool.Default) _routingKey = $"{_routingKey}.{(int)Pool}";

                return _routingKey;
            }
        }
    }

    /// <summary>
    /// Returns a <see cref="string"/> representation of the definition.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{ExchangeName} | {RoutingKey}";
}
