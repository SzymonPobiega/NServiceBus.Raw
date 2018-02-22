using System;
using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Settings;

public static class Helper
{
    public static void ConfigureASB(this TransportExtensions<AzureServiceBusTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
        extensions.ConnectionString(connectionString);


        var settings = extensions.GetSettings();
        var serializer = Tuple.Create(new FakeSerializer() as SerializationDefinition, new SettingsHolder());
        settings.Set("MainSerializer", serializer);
    }

    class FakeSerializer : SerializationDefinition
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            throw new NotImplementedException();
        }
    }
}