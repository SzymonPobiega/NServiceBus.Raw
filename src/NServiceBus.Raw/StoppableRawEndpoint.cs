namespace NServiceBus.Raw
{
    using System.Threading.Tasks;
    using Logging;
    using Settings;
    using Transport;

    class StoppableRawEndpoint : IStoppableRawEnedpoint
    {
        TransportInfrastructure transportInfrastructure;
        SettingsHolder settings;

        public StoppableRawEndpoint(TransportInfrastructure transportInfrastructure, SettingsHolder settings)
        {
            this.transportInfrastructure = transportInfrastructure;
            this.settings = settings;
        }

        public async Task Stop()
        {
            Log.Info("Initiating shutdown.");

            await transportInfrastructure.Stop().ConfigureAwait(false);
            settings.Clear();

            Log.Info("Shutdown complete.");
        }

        static ILog Log = LogManager.GetLogger<StoppableRawEndpoint>();
    }
}