using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_ASQ : When_sending_from_send_only_endpoint<SqsTransport>
{
    protected override void SetupTransport(TransportExtensions<SqsTransport> extensions)
    {
        extensions.ConfigureSQS();
    }
}