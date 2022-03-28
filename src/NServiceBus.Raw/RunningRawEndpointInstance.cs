using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    class RunningRawEndpointInstance : IReceivingRawEndpoint
    {
        public RunningRawEndpointInstance(
            string endpointName,
            RawTransportReceiver receiver,
            TransportInfrastructure transportInfrastructure)
        {
            this.receiver = receiver;
            this.transportInfrastructure = transportInfrastructure;
            EndpointName = endpointName;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellation = default)
        {
            return transportInfrastructure.Dispatcher.Dispatch(outgoingMessages, transaction);
        }

        public string ToTransportAddress(QueueAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        public async Task<IStoppableRawEndpoint> StopReceiving()
        {
            if (receiver != null)
            {
                Log.Info("Stopping receiver.");
                await receiver.Stop().ConfigureAwait(false);
                Log.Info("Receiver stopped.");
            }
            return new StoppableRawEndpoint(transportInfrastructure);
        }

        public string TransportAddress
        {
            get
            {
                return receiver.Receiver.ReceiveAddress;
            }
        }
        public string EndpointName { get; }
        public ISubscriptionManager SubscriptionManager
        {
            get
            {
                return receiver.Receiver.Subscriptions;
            }
        }

        public IReadOnlySettings Settings => throw new System.NotImplementedException();

        public async Task Stop()
        {
            var stoppable = await StopReceiving().ConfigureAwait(false);
            await stoppable.Stop();
        }

        TransportInfrastructure transportInfrastructure;
        RawTransportReceiver receiver;

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();
    }
}