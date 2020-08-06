using AcceptanceTests.RabbitMQ;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_RabbitMQ : When_publishing_to_another_endpoint<RabbitMQTransport>
{
    protected override void SetupTransport(TransportExtensions<RabbitMQTransport> extensions)
    {
        extensions.UseTestConnectionString();
        extensions.UseConventionalRoutingTopology();
    }
}