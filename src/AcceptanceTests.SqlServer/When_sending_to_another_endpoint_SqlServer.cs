using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_SqlServer : When_sending_to_another_endpoint<SqlServerTransport>
{
    protected override void SetupTransport(TransportExtensions<SqlServerTransport> extensions)
    {
        extensions.ConfigureSql();
    }
}