using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_SqlServer : When_sending_from_send_only_endpoint<SqlServerTransport>
{
    protected override void SetupTransport(TransportExtensions<SqlServerTransport> extensions)
    {
        extensions.ConfigureSql();
    }
}