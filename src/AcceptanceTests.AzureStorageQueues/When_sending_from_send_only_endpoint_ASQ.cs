using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_ASQ : When_sending_from_send_only_endpoint<AzureStorageQueueTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureASQ();
    }
}