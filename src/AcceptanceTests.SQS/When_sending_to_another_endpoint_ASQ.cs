using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_ASQ : When_sending_to_another_endpoint<SqsTransport>
{
    protected override void SetupTransport(TransportExtensions<SqsTransport> extensions)
    {
        extensions.ConfigureSQS();
    }
}