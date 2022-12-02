using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_SQS : When_sending_to_another_endpoint<SqsTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new SqsTransport();
    }
}