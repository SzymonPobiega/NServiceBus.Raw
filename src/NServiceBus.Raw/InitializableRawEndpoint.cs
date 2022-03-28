namespace NServiceBus.Raw
{
    using NServiceBus.Transport;
    using System.Threading.Tasks;

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(
            RawEndpointConfiguration rawEndpointConfiguration)
        {
            this.rawEndpointConfiguration = rawEndpointConfiguration;
        }

        public async Task<IStartableRawEndpoint> Initialize()
        {
            var criticalError = new RawCriticalError(null);
            var hostSettings = new HostSettings(
                "someHost", //TODO: what did the old version use?
                "some host display name",//TODO: what did the old version use?
                new StartupDiagnosticEntries(), //TODO: should we dump this somewhere?
                criticalError.Raise,
                rawEndpointConfiguration.setupInfrastructure,
                null); //null means "not hosted by core", transport SHOULD adjust accordingly to not assume things

            var receivers = new[]{
                new ReceiveSettings(
                    rawEndpointConfiguration.endpointName,
                    new QueueAddress(rawEndpointConfiguration.endpointName),
                    true,
                    false, //TODO: Purge was never supported by raw?
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