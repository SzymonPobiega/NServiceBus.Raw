using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_ASB : When_sending_from_send_only_endpoint<AzureServiceBusTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        extensions.ConfigureASB();
        extensions.UseForwardingTopology();
    }
}