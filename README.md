# PubSubClient

PubSubClient is a flexible and powerful publish-subscribe (pub/sub) messaging library designed for .NET applications. It enables easy integration of event-driven architectures with a variety of message brokers, allowing you to publish and subscribe to messages in a decoupled manner.

## Features

- **Broker-Agnostic**: Supports multiple message brokers such as RabbitMQ, Kafka, and others.
- **Easy Integration**: Designed to work seamlessly with ASP.NET Core and other .NET applications.
- **Asynchronous Processing**: Built-in support for asynchronous message processing.
- **Scalability**: Suitable for high-throughput, distributed environments.

## Installation

To install PubSubClient in your project, you can use the following NuGet command for your provider:
```bash
dotnet add package PubSubClient.ActiveMQ
dotnet add package PubSubClient.Kafka
dotnet add package PubSubClient.RabbitMQ
```

## Usage

### Basic Setup

 1. Configuration
 
 	PubSubClient expects the configuration for the broker connection in the `MessageBroker` section of the config.
    Example configurations provided below for each provider and links to documentation.

1. Register PubSubClient with the desired broker in your DI Container:
	```csharp
    using PubSubClient;
    using PubSubClient.RabbitMQ
	
	public class Startup
	{
	    public void ConfigureServices(IServiceCollection services)
	    {
	        services.AddPubSubClient<RabbitMqConsumerService, RabbitMqPublisherService>(); // Example using RabbitMQ
	    }
	}
    ```

1. Define a Service Definition:
	```csharp
    using PubSubClient;
    
    public class MyServiceDefinition : MicroServiceDefinitionBase
    {
    	public override string ExchangeName => "MyExchange";
    }
    ```
    
1. Publish a message:
	```csharp
	public class MyPublisherService
	{
	    private readonly IPublisherService _publisher;
	
	    public MyPublisherService(IPublisherService publisher)
	    {
	        _publisher = publisher;
	    }
	
	    public async Task PublishMessageAsync()
	    {
	        var message = new MyMessage { MessageContent = "Hello, PubSub!" };
	        await _publisher.PublishAsync(message, new MyServiceDefinition());
	    }
	}
    ```

1. Consume messages:
	```csharp
    using PubSubClient.Attributes;
	
    public class Consumers
    {
	    [ConsumerMethod(typeof(MyServiceDefinition))]
	    public async Task RunMyMethod(object o, MyMessage message)
	    {
	        await Task.FromResult(0);
	        Console.WriteLine($"Received message: {message.MessageContent}");
	    }
	}
    ```
    *Note: The consumer method must a signature of (object, T) or (T).  The object passed is the entire message with metadata from the broker.*
    
## Supported Message Brokers

PubSubClient supports multiple brokers. Each broker can be installed as a separate NuGet package:
* **RabbitMQ:** A popular message broker for reliable messaging.
	
    ```json
    "MessageBroker": {
  	  "HostName": "localhost:5672",
  	  "DispatchConsumersAsync": true
	}
    ```
    
    See [here](https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.ConnectionFactory.html#properties) for documentation.

* **Kafka:** High-throughput, distributed streaming platform.
	
    ```json
	"MessageBroker": {
  	  "BootstrapServers": "localhost:9092",
  	  "saslUsername": "admin",
  	  "saslPassword": "admin123",
  	  "SecurityProtocol": 2,
  	  "SaslMechanism": 1,
  	  "AutoOffsetReset": 1
	}
    ```
    
    See [here](https://docs.confluent.io/platform/current/clients/confluent-kafka-dotnet/_site/api/Confluent.Kafka.ConsumerConfig.html#properties) for documentation.

* **Apache ActiveMQ:** Open-source, multi-protocol distributed messaging platform.
	
    ```json
	"MessageBroker": {
  	  "MqServerName": "activemq:tcp://localhost:61616",
  	  "MqUserName": "artemis",
  	  "MqPassword": "artemis",
	}
    ```
    
    *Note: These are the only configuration options available for ActiveMQ*

### Adding Custom Brokers

You can also implement your own custom consumer and publisher by creating a class that implements the `IConsumerService` and `IPublisherService` interfaces.
```csharp
public class MyCustomConsumerService : IConsumerService
{
    // Implementation goes here
}

public class MyCustomPublisherService : IPublisherService
{
    // Implementation goes here
}
```

## License
PubSubClient is licensed under the GPL License. See the [LICENSE](https://spdx.org/licenses/GPL-3.0-or-later.html) for more information.

## Contributing
Contributions are always welcome! Feel free to open an issue or submit a pull request.