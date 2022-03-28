using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Transport.RabbitMQ;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_RabbitMQ : When_publishing_to_another_endpoint<RabbitMQTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new RabbitMQTransport(new ConventionalRoutingTopology(true), "host=localhost");
    }
}