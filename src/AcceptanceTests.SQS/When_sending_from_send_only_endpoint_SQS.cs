using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_SQS : When_sending_from_send_only_endpoint<SqsTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new SqsTransport();
    }
}