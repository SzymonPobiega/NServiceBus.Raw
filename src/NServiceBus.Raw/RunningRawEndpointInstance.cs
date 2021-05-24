using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class RunningRawEndpointInstance : IReceivingRawEndpoint
    {
        public RunningRawEndpointInstance(SettingsHolder settings, RawTransportReceiver receiver, TransportInfrastructure transportInfrastructure, IMessageDispatcher dispatcher, ISubscriptionManager subscriptionManager, string localAddress)
        {
            this.settings = settings;
            this.receiver = receiver;
            this.transportInfrastructure = transportInfrastructure;
            this.dispatcher = dispatcher;
            this.TransportAddress = localAddress;
            SubscriptionManager = subscriptionManager;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }

        public string ToTransportAddress(QueueAddress queueAddress)
        {
            return transportInfrastructure.ToTransportAddress(queueAddress);
        }

        public async Task<IStoppableRawEndpoint> StopReceiving(CancellationToken cancellationToken)
        {
            if (receiver != null)
            {
                Log.Info("Stopping receiver.");
                await receiver.Stop(cancellationToken).ConfigureAwait(false);
                Log.Info("Receiver stopped.");
            }
            return new StoppableRawEndpoint(transportInfrastructure, settings);
        }

        public string TransportAddress { get; }
        public string EndpointName => settings.EndpointName();
        public ReadOnlySettings Settings => settings;
        public ISubscriptionManager SubscriptionManager { get; }

        public async Task Stop(CancellationToken cancellationToken)
        {
            var stoppable = await StopReceiving(cancellationToken).ConfigureAwait(false);
            await stoppable.Stop(cancellationToken).ConfigureAwait(false);
        }

        TransportInfrastructure transportInfrastructure;
        IMessageDispatcher dispatcher;

        SettingsHolder settings;
        RawTransportReceiver receiver;

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();
    }
}