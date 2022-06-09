using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class WWhen_pausing_and_resuming_Msmq : When_pausing_and_resuming<MsmqTransport>
{
    protected override void SetupTransport(TransportExtensions<MsmqTransport> extensions)
    {
    }
}