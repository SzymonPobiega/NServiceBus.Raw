using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_ASB : When_publishing_to_another_endpoint
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        extensions.ConfigureASB();
        extensions.UseForwardingTopology();
    }

    protected override TransportDefinition SetupTransport()
    {
        return new AzureServiceBusTransport();
    }
}

