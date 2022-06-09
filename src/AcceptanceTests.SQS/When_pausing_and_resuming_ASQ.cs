using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_ASQ : When_pausing_and_resuming<SqsTransport>
{
    protected override void SetupTransport(TransportExtensions<SqsTransport> extensions)
    {
        extensions.ConfigureSQS();
    }
}