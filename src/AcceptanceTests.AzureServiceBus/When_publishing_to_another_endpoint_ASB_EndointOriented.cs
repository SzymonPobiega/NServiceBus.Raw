using System;
using NServiceBus;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_ASB_EndointOriented : When_publishing_to_another_endpoint<AzureServiceBusTransport>
{
    protected override void SetupTransport(TransportExtensions<AzureServiceBusTransport> extensions)
    {
        var connectionString = EnvironmentHelper.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
        extensions.ConnectionString(connectionString);

        var topology = extensions.UseEndpointOrientedTopology();
        topology.RegisterPublisher(typeof(MyEvent), "Publisher");

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

