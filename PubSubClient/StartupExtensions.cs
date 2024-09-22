using PubSubClient.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PubSubClient;

public static class StartupExtensions
{
    /// <summary>
    /// Adds consumer service(s) to the application's dependency injection container.
    /// </summary>
    /// <typeparam name="TConsumerService">The type of the consumer provider to add, which must implement <see cref="IConsumerService"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to which the consumer services is added.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// This method registers the methods marked by <see cref="ConsumerMethodAttribute"/> to the <see cref="AsyncConsumerBackgroundService"/>.
    /// </remarks>
    public static IHostApplicationBuilder AddConsumerServices<TConsumerService>(this IHostApplicationBuilder builder)
    where TConsumerService : IConsumerService
    {
        builder.Services.AddSingleton<AsyncConsumerBackgroundService>();

        IEnumerable<MethodInfo>? consumerMethods = Assembly.GetEntryAssembly()?.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.GetCustomAttributes(typeof(ConsumerMethodAttribute), false).Length != 0));

        foreach (Type? c in consumerMethods?.Select(m => m.DeclaringType)?.Distinct() ?? new List<Type>())
        {
            if (c is not null) builder.Services.AddSingleton(c);
        }

        builder.Services.AddHostedService(provider =>
        {
            var service = provider.GetRequiredService<AsyncConsumerBackgroundService>();
            var logger = provider.GetRequiredService<ILogger<TConsumerService>>();

            foreach (MethodInfo? method in consumerMethods ?? new List<MethodInfo>())
            {
                if (method is null || method.DeclaringType is null) continue;

                var instance = provider.GetRequiredService(method.DeclaringType);
                var attr = (ConsumerMethodAttribute)method.GetCustomAttributes(typeof(ConsumerMethodAttribute), false).First();
                try
                {
                    var func = CreateFuncFromMethodInfo(method, instance);
                    object[] parameters = new object[] { logger, builder.Configuration, attr.ServiceDefinition, func };
                    ConstructorInfo? constructor = typeof(TConsumerService).GetConstructors().FirstOrDefault(ctor => IsMatchingConstructor(ctor, parameters));
                    if (constructor is not null)
                    {
                        IConsumerService? consumerService = constructor.Invoke(parameters) as IConsumerService;
                        if (consumerService is not null) service.AddConsumer(consumerService);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error initializing consumer methods.");
                }
            }

            return service;
        });

        return builder;
    }

    /// <summary>
    /// Adds a publisher service(s) to the application's dependency injection container.
    /// </summary>
    /// <typeparam name="TPublisherService">The type of the publisher provider to add, which must implement <see cref="IPublisherService"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to which the publisher service is added.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// This method registers the specified caching provider type <typeparamref name="TPublisherService"/> as a singleton in the service collection.
    /// </remarks>
    public static IHostApplicationBuilder AddPublisherService<TPublisherService>(this IHostApplicationBuilder builder)
        where TPublisherService : class, IPublisherService
    {
        builder.Services.AddSingleton<IPublisherService, TPublisherService>();

        return builder;
    }

    static Delegate CreateFuncFromMethodInfo(MethodInfo methodInfo, object instance)
    {
        if (methodInfo.ReturnType != typeof(Task)) throw new ArgumentException($"Method '{methodInfo.DeclaringType}.{methodInfo.Name}' must return type of Task.");
        var parameters = methodInfo.GetParameters();
        if (parameters.Length < 1 || parameters.Length > 2)
            throw new ArgumentException($"Method '{methodInfo.DeclaringType}.{methodInfo.Name}' must take either 'exactly one parameter of expected type' or 'exactly two parameters with the first being an object and second being expected type'.");
        if (parameters.Length == 2 && parameters[0].ParameterType != typeof(object))
            throw new ArgumentException($"Method '{methodInfo.DeclaringType}.{methodInfo.Name}' must take either 'exactly one parameter of expected type' or 'exactly two parameters with the first being an object and second being expected type'.");

        if (parameters.Length == 1)
        {
            Type inputType = parameters[0].ParameterType;
            var funcType = typeof(Func<,>).MakeGenericType(inputType, typeof(Task));

            return Delegate.CreateDelegate(funcType, instance, methodInfo);
        }
        else
        {
            Type inputType = parameters[1].ParameterType;
            var funcType = typeof(Func<,,>).MakeGenericType(typeof(object), inputType, typeof(Task));

            return Delegate.CreateDelegate(funcType, instance, methodInfo);
        }
    }

    static bool IsMatchingConstructor(ConstructorInfo ctor, object[] parameters)
    {
        ParameterInfo[] paramInfos = ctor.GetParameters();
        if (paramInfos.Length != parameters.Length) return false;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (!paramInfos[i].ParameterType.IsAssignableFrom(parameters[i].GetType())) return false;
        }

        return true;
    }
}
