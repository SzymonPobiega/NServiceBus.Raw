using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public static class Helper
{
    public static void ConfigureASB(this TransportExtensions<AzureServiceBusTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
        extensions.ConnectionString(connectionString);
    }
}