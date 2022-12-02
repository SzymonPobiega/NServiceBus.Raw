using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Transport.RabbitMQ;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_RabbitMQ_Direct : When_publishing_to_another_endpoint<RabbitMQTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new RabbitMQTransport(RoutingTopology.Direct(QueueType.Quorum), "host=localhost");
    }
}