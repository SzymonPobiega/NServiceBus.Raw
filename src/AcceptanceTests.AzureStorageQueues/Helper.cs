using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public static class Helper
{
    public static AzureStorageQueueTransport ConfigureASQ()
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureStorageQueues.ConnectionString");

        return new AzureStorageQueueTransport(connectionString)
        {
            MessageWrapperSerializationDefinition = new NewtonsoftJsonSerializer()
        };
    }
}