using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_using_delayed_retries : When_using_delayed_retries<MsmqTransport>
{
    protected override void SetupTransport(TransportExtensions<MsmqTransport> extensions)
    {
    }
}