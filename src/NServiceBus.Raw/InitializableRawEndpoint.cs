namespace NServiceBus.Raw
{
    using NServiceBus.Transport;
    using System.Threading.Tasks;

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(RawEndpointConfiguration rawEndpointConfiguration)
        {
            this.rawEndpointConfiguration = rawEndpointConfiguration;
        }

        public async Task<IStartableRawEndpoint> Initialize()
        {
            var criticalError = new RawCriticalError(rawEndpointConfiguration.OnCriticalError);

            var hostSettings = new HostSettings(
                rawEndpointConfiguration.EndpointName,
                "NServiceBus.Raw host for " + rawEndpointConfiguration.EndpointName,
                new StartupDiagnosticEntries(),
                criticalError.Raise,
                rawEndpointConfiguration.SetupInfrastructure,
                null); //null means "not hosted by core", transport SHOULD adjust accordingly to not assume things

            var usePubSub = rawEndpointConfiguration.TransportDefinition.SupportsPublishSubscribe && !rawEndpointConfiguration.PublishAndSubscribeDisabled;
            var receivers = new[]{
                new ReceiveSettings(
                    rawEndpointConfiguration.EndpointName,
                    new QueueAddress(rawEndpointConfiguration.EndpointName),
                    usePubSub,
                    false,
                    rawEndpointConfiguration.PoisonMessageQueue)};

            var transportInfrastructure = await rawEndpointConfiguration.TransportDefinition.Initialize(
                hostSettings,
                receivers,
                rawEndpointConfiguration.AdditionalQueues);

            var startableEndpoint = new StartableRawEndpoint(
                rawEndpointConfiguration,
                transportInfrastructure,
                criticalError);
            return startableEndpoint;
        }

        readonly RawEndpointConfiguration rawEndpointConfiguration;
    }
}