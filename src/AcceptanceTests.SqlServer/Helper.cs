using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public static class Helper
{
    public static void ConfigureSql(this TransportExtensions<SqlServerTransport> extensions)
    {
        extensions.ConnectionString(EnvironmentHelper.GetEnvironmentVariable("SqlServerTransportConnectionString"));
        extensions.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
    }
}