namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class StoppableRawEndpoint : IStoppableRawEndpoint
    {
        TransportInfrastructure transportInfrastructure;

        public StoppableRawEndpoint(TransportInfrastructure transportInfrastructure)
        {
            this.transportInfrastructure = transportInfrastructure;
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            Log.Info("Initiating shutdown.");

            try
            {
                await transportInfrastructure.Shutdown(cancellationToken).ConfigureAwait(false);
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

        static ILog Log = LogManager.GetLogger<StoppableRawEndpoint>();
    }
}