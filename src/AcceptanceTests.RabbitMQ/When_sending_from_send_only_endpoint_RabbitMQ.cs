using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_RabbitMQ : When_sending_from_send_only_endpoint<RabbitMQTransport>
{
    protected override void SetupTransport(TransportExtensions<RabbitMQTransport> extensions)
    {
        extensions.ConnectionString("host=localhost");
        extensions.UseConventionalRoutingTopology();
    }
}