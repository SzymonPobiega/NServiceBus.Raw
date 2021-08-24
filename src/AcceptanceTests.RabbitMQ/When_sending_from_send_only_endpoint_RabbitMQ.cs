using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Transport.RabbitMQ;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_RabbitMQ : When_sending_from_send_only_endpoint
{
    protected override TransportDefinition SetupTransport()
    {
        return new RabbitMQTransport(new ConventionalRoutingTopology(true), "host=localhost");
    }
}