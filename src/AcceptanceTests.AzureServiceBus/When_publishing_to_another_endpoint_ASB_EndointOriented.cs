using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_ASB_EndointOriented : When_publishing_to_another_endpoint<AzureServiceBusTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        extensions.ConfigureASB();
        
        var topology = extensions.UseEndpointOrientedTopology();
        topology.RegisterPublisher(typeof(MyEvent), "Publisher");
    }
}

