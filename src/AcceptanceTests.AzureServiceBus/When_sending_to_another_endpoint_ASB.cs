using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_ASB : When_sending_to_another_endpoint<AzureServiceBusTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        extensions.ConfigureASB();
        extensions.UseForwardingTopology();
    }
}