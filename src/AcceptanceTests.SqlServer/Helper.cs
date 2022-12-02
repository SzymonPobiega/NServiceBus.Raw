using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Transport;

public static class Helper
{
    public static TransportDefinition ConfigureSql()
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("SqlServerTransportConnectionString");

        return new SqlServerTransport(connectionString)
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        };
    }
}