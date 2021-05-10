using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public static class Helper
{
    public static void ConfigureASQ(this TransportExtensions<AzureStorageQueueTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureStorageQueues.ConnectionString");
        extensions.ConnectionString(connectionString);
    }
}