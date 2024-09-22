using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace PubSubClient.Attributes;

/// <summary>
/// An attribute used to specify a consumer method to be executed when a message with the defined definition is received.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ConsumerMethodAttribute : Attribute
{
    private static readonly ConcurrentDictionary<Type, Func<IMicroServiceDefinition>> _serviceDefinitionConstructorDelegates = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerMethodAttribute"/> class.
    /// This attribute is used to decorate a method that is executed when a message is received.
    /// The <paramref name="serviceDefinitionType"/> must implement the <see cref="IMicroServiceDefinition"/>
    /// interface to ensure that only compatible message handlers are used.
    /// </summary>
    /// <param name="serviceDefinitionType">Specified type must implement <see cref="IMicroServiceDefinition" /></param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serviceDefinitionType"/> does not implement <see cref="IMicroServiceDefinition"/>.
    /// </exception>
    public ConsumerMethodAttribute(Type serviceDefinitionType)
    {
        ServiceDefinition = CreateMicroServiceDefinition(serviceDefinitionType);
    }

    /// <summary>
    /// The definition used by the method to receive messages as a consumer.
    /// </summary>
    public virtual IMicroServiceDefinition ServiceDefinition { get; protected set; }

    static IMicroServiceDefinition CreateMicroServiceDefinition(Type serviceDefinitionType)
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

    /// <inheritdoc/>
    public override string ToString()
    {
        return $@"{nameof(ServiceDefinition.ExchangeName)}: {ServiceDefinition.ExchangeName}
{nameof(ServiceDefinition.RoutingKey)}: {ServiceDefinition.RoutingKey}";
    }
}