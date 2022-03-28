using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_using_delayed_retries : When_using_delayed_retries<MsmqTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return new MsmqTransport();
    }
}