using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Transport;

public static class Helper
{
    public static TransportDefinition SetupSqlTransport()
    {
        var transport = new SqlServerTransport(EnvironmentHelper.GetEnvironmentVariable("SQLServerConnectionString"))
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        };
        return transport;
    }
}