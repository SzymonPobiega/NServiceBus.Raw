using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_SqlServer : When_sending_to_another_endpoint
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.SetupSqlTransport();
    }
}