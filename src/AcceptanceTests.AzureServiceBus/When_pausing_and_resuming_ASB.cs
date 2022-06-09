using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_ASB : When_pausing_and_resuming<AzureServiceBusTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        extensions.ConfigureASB();
        extensions.UseForwardingTopology();
    }
}