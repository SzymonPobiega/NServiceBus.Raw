using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_Msmq : When_sending_to_another_endpoint<MsmqTransport>
{
    protected override void SetupTransport(TransportExtensions<MsmqTransport> extensions)
    {
    }
}