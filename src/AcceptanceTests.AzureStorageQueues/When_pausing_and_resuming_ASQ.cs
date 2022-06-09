using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_ASQ : When_pausing_and_resuming<AzureStorageQueueTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureStorageQueueTransport> extensions)
    {
        extensions.ConfigureASQ();
    }
}