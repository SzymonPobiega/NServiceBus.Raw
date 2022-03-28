namespace NServiceBus.Raw
{
    using NServiceBus.Logging;
    using NServiceBus.Transport;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(
            RawEndpointConfiguration rawEndpointConfiguration,
            TransportInfrastructure transportInfrastructure,
            RawCriticalError criticalError)
        {
            this.criticalError = criticalError;
            this.messagePump = transportInfrastructure.Receivers.Values.First();
            this.dispatcher = transportInfrastructure.Dispatcher;
            this.rawEndpointConfiguration = rawEndpointConfiguration;
            this.transportInfrastructure = transportInfrastructure;
            SubscriptionManager = messagePump.Subscriptions;
            TransportAddress = messagePump.ReceiveAddress;
        }

        public async Task<IReceivingRawEndpoint> Start()
        {
            if (startHasBeenCalled)
            {
                throw new InvalidOperationException("Multiple calls to Start is not supported.");
            }

            startHasBeenCalled = true;

            RawTransportReceiver receiver = null;

            if (!rawEndpointConfiguration.SendOnly)
            {
                receiver = BuildMainReceiver();

                if (receiver != null)
                {
                    await InitializeReceiver(receiver).ConfigureAwait(false);
                }
            }

            var runningInstance = new RunningRawEndpointInstance(EndpointName, receiver, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            try
            {
                if (receiver != null)
                {
                    StartReceiver(receiver);
                }

                return runningInstance;
            }
            catch
            {
                await runningInstance.Stop();

                throw;
            }
        }

        public ISubscriptionManager SubscriptionManager { get; }

        public string TransportAddress { get; }
        public string EndpointName => rawEndpointConfiguration.EndpointName;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return dispatcher.Dispatch(outgoingMessages, transaction);
        }

        public string ToTransportAddress(QueueAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        static void StartReceiver(RawTransportReceiver receiver)
        {
            try
            {
                receiver.Start();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to start.", ex);
                throw;
            }
        }

        static async Task InitializeReceiver(RawTransportReceiver receiver)
        {
            try
            {
                await receiver.Init().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to initialize.", ex);
                throw;
            }
        }

        RawTransportReceiver BuildMainReceiver()
        {
            var endpointName = rawEndpointConfiguration.EndpointName;
            var receiver = new RawTransportReceiver(
                messagePump,
                dispatcher,
                rawEndpointConfiguration.OnMessage,
                rawEndpointConfiguration.PushRuntimeSettings,
                new RawEndpointErrorHandlingPolicy(endpointName, endpointName, dispatcher, rawEndpointConfiguration.ErrorHandlingPolicy));
            return receiver;
        }

        readonly RawEndpointConfiguration rawEndpointConfiguration;
        TransportInfrastructure transportInfrastructure;
        RawCriticalError criticalError;
        IMessageReceiver messagePump;
        IMessageDispatcher dispatcher;
        bool startHasBeenCalled;
        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}