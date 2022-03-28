namespace NServiceBus.Raw
{
    using Logging;
    using System.Threading.Tasks;
    using Transport;

    class StoppableRawEndpoint : IStoppableRawEndpoint
    {

        public StoppableRawEndpoint(TransportInfrastructure transportInfrastructure)
        {
            this.transportInfrastructure = transportInfrastructure;
        }

        public async Task Stop()
        {
            Log.Info("Initiating shutdown.");

            try
            {
                await transportInfrastructure.Shutdown().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                //Ignore when shutting down
            }
            finally
            {
                Log.Info("Shutdown complete.");
            }
        }

        TransportInfrastructure transportInfrastructure;

        static ILog Log = LogManager.GetLogger<StoppableRawEndpoint>();
    }
}