using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_RabbitMQ_Direct : When_publishing_to_another_endpoint<RabbitMQTransport>
{
    protected override void SetupTransport(TransportExtensions<RabbitMQTransport> extensions)
    {
        extensions.ConnectionString("host=localhost");
        extensions.UseDirectRoutingTopology();
    }
}