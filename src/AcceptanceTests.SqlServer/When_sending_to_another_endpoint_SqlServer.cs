using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_SqlServer : When_sending_to_another_endpoint<SqlServerTransport>
{
    protected override void SetupTransport(TransportExtensions<SqlServerTransport> extensions)
    {
        extensions.ConnectionString(EnvironmentHelper.GetEnvironmentVariable("SqlServerTransportConnectionString"));
        extensions.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
    }
}