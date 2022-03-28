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
            //TODO: stop using this
            var criticalError = new RawCriticalError(null);
            var hostSettings = new HostSettings(
                rawEndpointConfiguration.endpointName,
                "NServiceBus.Raw host for " + rawEndpointConfiguration.endpointName,
                new StartupDiagnosticEntries(),
                criticalError.Raise,
                rawEndpointConfiguration.setupInfrastructure,
                null); //null means "not hosted by core", transport SHOULD adjust accordingly to not assume things

            var usePubSub = rawEndpointConfiguration.transportDefinition.SupportsPublishSubscribe && !rawEndpointConfiguration.disablePublishAndSubscribe;
            var receivers = new[]{
                new ReceiveSettings(
                    rawEndpointConfiguration.endpointName,
                    new QueueAddress(rawEndpointConfiguration.endpointName),
                    usePubSub,
                    false,
                    rawEndpointConfiguration.poisonMessageQueue)};

            var transportInfrastructure = await rawEndpointConfiguration.transportDefinition.Initialize(
                hostSettings,
                receivers,
                rawEndpointConfiguration.additionalQueues);

            var startableEndpoint = new StartableRawEndpoint(
                rawEndpointConfiguration,
                transportInfrastructure,
                criticalError);
            return startableEndpoint;
        }

        readonly RawEndpointConfiguration rawEndpointConfiguration;
    }
}