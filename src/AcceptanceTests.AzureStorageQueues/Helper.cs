using System;
using System.Reflection;
using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Serialization;
using NServiceBus.Settings;
using NServiceBus.Unicast.Messages;

public static class Helper
{
    public static void ConfigureASQ(this TransportExtensions<AzureStorageQueueTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureStorageQueues.ConnectionString");
        extensions.ConnectionString(connectionString);

        var settings = extensions.GetSettings();
        var serializer = Tuple.Create(new NewtonsoftSerializer() as SerializationDefinition, new SettingsHolder());
        settings.Set("MainSerializer", serializer);

        bool isMessageType(Type t) => t == typeof(MessageWrapper);

        var ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Func<Type, bool>) }, null);
        settings.Set<MessageMetadataRegistry>(ctor.Invoke(new object[] { (Func<Type, bool>)isMessageType }));
    }
}