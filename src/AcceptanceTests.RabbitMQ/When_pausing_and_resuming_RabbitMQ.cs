using AcceptanceTests.RabbitMQ;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_RabbitMQ : When_pausing_and_resuming<RabbitMQTransport>
{
    protected override void SetupTransport(TransportExtensions<RabbitMQTransport> extensions)
    {
        extensions.UseTestConnectionString();
        extensions.UseConventionalRoutingTopology();
    }
}