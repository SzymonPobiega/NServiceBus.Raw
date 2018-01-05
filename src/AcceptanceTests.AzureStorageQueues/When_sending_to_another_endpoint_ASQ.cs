using System;
using System.Reflection;
using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Serialization;
using NServiceBus.Settings;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_ASQ : When_sending_to_another_endpoint<AzureStorageQueueTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureStorageQueueTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureStorageQueues.ConnectionString");
        extensions.ConnectionString(connectionString);

        var settings = extensions.GetSettings();
        var serializer = Tuple.Create(new NewtonsoftSerializer() as SerializationDefinition, new SettingsHolder());
        settings.Set("MainSerializer", serializer);

        bool isMessageType(Type t) => t == typeof(MessageWrapper);

        var ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[] {typeof(Func<Type, bool>)}, null);
        settings.Set<MessageMetadataRegistry>(ctor.Invoke(new object[]{(Func<Type, bool>)isMessageType}));
    }
}

