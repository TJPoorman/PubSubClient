using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace PubSubClient.Attributes;

/// <summary>
/// An attribute used to specify the behavior of the consumer that should pass messages to this method
/// </summary>
/// <remarks>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ConsumerMethodAttribute : Attribute
{
    private static readonly ConcurrentDictionary<Type, Func<IMicroServiceDefinition>> _serviceDefinitionConstructorDelegates = new();
    private int _hashCode = 0;

    /// <param name="serviceDefinitionType">Specified type must implement <see cref="IMicroServiceDefinition" /></param>
    public ConsumerMethodAttribute(Type serviceDefinitionType)
    {
        ServiceDefinition = CreateMicroServiceDefinition(serviceDefinitionType);
    }

    public virtual IMicroServiceDefinition ServiceDefinition { get; protected set; }

    public static IMicroServiceDefinition CreateMicroServiceDefinition(Type serviceDefinitionType)
    {
        if (!typeof(IMicroServiceDefinition).IsAssignableFrom(serviceDefinitionType))
        {
            throw new ArgumentException($"{serviceDefinitionType.Name} must implement {nameof(IMicroServiceDefinition)}.", nameof(serviceDefinitionType));
        }

        if (!_serviceDefinitionConstructorDelegates.TryGetValue(serviceDefinitionType, out Func<IMicroServiceDefinition>? constructorDelegate))
        {
            ConstructorInfo? constructorInfo = serviceDefinitionType.GetConstructor(Type.EmptyTypes) ??
                throw new ArgumentException($"{serviceDefinitionType.Name} must have a public default constructor.", nameof(serviceDefinitionType));
            NewExpression constructorExpression = Expression.New(constructorInfo);
            UnaryExpression unaryExpression = Expression.Convert(constructorExpression, typeof(IMicroServiceDefinition));
            LambdaExpression lambdaExpression = Expression.Lambda(unaryExpression);

            constructorDelegate = (Func<IMicroServiceDefinition>)lambdaExpression.Compile();
            _serviceDefinitionConstructorDelegates.TryAdd(serviceDefinitionType, constructorDelegate);
        }

        return constructorDelegate();
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        return _hashCode == obj.GetHashCode();
    }

    public override int GetHashCode()
    {
        if (_hashCode == 0)
        {
            _hashCode = new
            {
                ServiceDefinition?.ExchangeName,
                ServiceDefinition?.RoutingKey
            }.GetHashCode();
        }

        return _hashCode;
    }

    public override string ToString()
    {
        return $@"{nameof(ServiceDefinition.ExchangeName)}: {ServiceDefinition.ExchangeName}
{nameof(ServiceDefinition.RoutingKey)}: {ServiceDefinition.RoutingKey}";
    }
}