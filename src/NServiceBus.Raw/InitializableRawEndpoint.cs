using NServiceBus.Settings;
using NServiceBus.Transport;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Raw
{

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(
            SettingsHolder settings,
            RawEndpointConfiguration rawEndpointConfiguration)
        {
            this.settings = settings;
            this.rawEndpointConfiguration = rawEndpointConfiguration;
        }

        public async Task<IStartableRawEndpoint> Initialize()
        {
            var setupInfrastructure = true;
            var hostSettings = new HostSettings(
                "someHost",
                "some host display name",
                new StartupDiagnosticEntries(),
                (m, ex, ct) => { Console.WriteLine(ex.ToString()); },
                setupInfrastructure,
                null); //null means "not hosted by core", transport SHOULD adjust accordingly to not assume things

            var receivers = new[]{
                new ReceiveSettings(
                    "myInputQueue",
                    new QueueAddress("myInputQueue"),
                    true,
                    false,
                    "error")};

            var transportInfrastructure = await rawEndpointConfiguration.transportDefinition.Initialize(
                hostSettings,
                receivers,
                new string[] { "todo" });

            var criticalError = new RawCriticalError(null);
            var startableEndpoint = new StartableRawEndpoint(
                settings,
                rawEndpointConfiguration,
                transportInfrastructure,
                criticalError,
                rawEndpointConfiguration.onMessage);
            return startableEndpoint;
        }

        readonly SettingsHolder settings;
        readonly RawEndpointConfiguration rawEndpointConfiguration;
    }
}