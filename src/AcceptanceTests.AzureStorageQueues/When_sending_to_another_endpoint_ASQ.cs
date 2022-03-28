using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_ASQ : When_sending_to_another_endpoint<AzureStorageQueueTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureASQ();
    }
}