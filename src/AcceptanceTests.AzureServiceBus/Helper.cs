using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public static class Helper
{
    public static AzureServiceBusTransport ConfigureASB()
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

        return new AzureServiceBusTransport(connectionString);
    }
}