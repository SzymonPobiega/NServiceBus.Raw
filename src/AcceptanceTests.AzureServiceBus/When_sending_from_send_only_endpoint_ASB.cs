using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_ASB : When_sending_from_send_only_endpoint<AzureServiceBusTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureASB();
    }
}