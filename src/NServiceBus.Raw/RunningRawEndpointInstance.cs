using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class RunningRawEndpointInstance : IReceivingRawEndpoint
    {
        public RunningRawEndpointInstance(RawTransportReceiver receiver, TransportDefinition transportDefinition, TransportInfrastructure transportInfrastructure, IMessageDispatcher dispatcher, ISubscriptionManager subscriptionManager, string endpointName, string transportAddress)
        {
            this.receiver = receiver;
            this.transportDefinition = transportDefinition;
            this.transportInfrastructure = transportInfrastructure;
            this.dispatcher = dispatcher;
            this.endpointName = endpointName;
            this.TransportAddress = transportAddress;
            SubscriptionManager = subscriptionManager;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }

        public string ToTransportAddress(QueueAddress queueAddress)
        {
            return transportDefinition.ToTransportAddress(queueAddress);
        }

        public async Task<IStoppableRawEndpoint> StopReceiving(CancellationToken cancellationToken)
        {
            if (receiver != null)
            {
                Log.Info("Stopping receiver.");
                await receiver.Stop(cancellationToken).ConfigureAwait(false);
                Log.Info("Receiver stopped.");
            }
            return new StoppableRawEndpoint(transportInfrastructure);
        }

        public string TransportAddress { get; }
        public string EndpointName => endpointName;
        public ISubscriptionManager SubscriptionManager { get; }

        public async Task Stop(CancellationToken cancellationToken)
        {
            var stoppable = await StopReceiving(cancellationToken).ConfigureAwait(false);
            await stoppable.Stop(cancellationToken).ConfigureAwait(false);
        }

        TransportInfrastructure transportInfrastructure;
        IMessageDispatcher dispatcher;
        readonly string endpointName;

        RawTransportReceiver receiver;
        readonly TransportDefinition transportDefinition;

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();
    }
}