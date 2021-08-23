using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(TransportDefinition transportDefinition, TransportInfrastructure transportInfrastructure, RawTransportReceiver receiver, string endpointName, string transportAddress)
        {
            this.transportDefinition = transportDefinition;
            this.transportInfrastructure = transportInfrastructure;
            this.receiver = receiver;
            this.endpointName = endpointName;
            this.transportAddress = transportAddress;
        }

        public async Task<IReceivingRawEndpoint> Start(CancellationToken cancellationToken)
        {
            if (receiver != null)
            {
                await InitializeReceiver(receiver, cancellationToken).ConfigureAwait(false);
            }

            var runningInstance = new RunningRawEndpointInstance(receiver, transportDefinition, transportInfrastructure, transportInfrastructure.Dispatcher, SubscriptionManager, endpointName, transportAddress);

            if (receiver != null)
            {
                StartReceiver(receiver, cancellationToken);
            }
            return runningInstance;
        }

        public ISubscriptionManager SubscriptionManager { get; }

        public string TransportAddress => transportAddress;
        public string EndpointName => endpointName;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
        {
            return transportInfrastructure.Dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }

        public string ToTransportAddress(QueueAddress queueAddress)
        {
            return transportDefinition.ToTransportAddress(queueAddress);
        }

        static void StartReceiver(RawTransportReceiver receiver, CancellationToken cancellationToken)
        {
            try
            {
                receiver.Start(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to start.", ex);
                throw;
            }
        }

        static async Task InitializeReceiver(RawTransportReceiver receiver, CancellationToken cancellationToken)
        {
            try
            {
                await receiver.Initialize(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to initialize.", ex);
                throw;
            }
        }

        readonly TransportInfrastructure transportInfrastructure;
        readonly RawTransportReceiver receiver;
        readonly TransportDefinition transportDefinition;
        readonly string endpointName;
        readonly string transportAddress;

        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}