using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_Msmq : When_sending_from_send_only_endpoint<MsmqTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new MsmqTransport();
    }
}