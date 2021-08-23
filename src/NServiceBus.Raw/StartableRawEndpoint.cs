using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(SettingsHolder settings, TransportInfrastructure transportInfrastructure,
            RawCriticalError criticalError, IMessageReceiver messagePump, IMessageDispatcher dispatcher,
            ISubscriptionManager subscriptionManager, Func<MessageContext, IMessageDispatcher, Task> onMessage, string localAddress)
        {
            this.criticalError = criticalError;
            this.messagePump = messagePump;
            this.dispatcher = dispatcher;
            this.onMessage = onMessage;
            this.localAddress = localAddress;
            this.settings = settings;
            this.transportInfrastructure = transportInfrastructure;
            SubscriptionManager = subscriptionManager;
        }

        public async Task<IReceivingRawEndpoint> Start(CancellationToken cancellationToken)
        {
            var receiver = CreateReceiver();

            if (receiver != null)
            {
                await InitializeReceiver(receiver, cancellationToken).ConfigureAwait(false);
            }

            var runningInstance = new RunningRawEndpointInstance(settings, receiver, transportInfrastructure, dispatcher, SubscriptionManager, localAddress);
            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance, cancellationToken);

            if (receiver != null)
            {
                StartReceiver(receiver, cancellationToken);
            }
            return runningInstance;
        }

        public ISubscriptionManager SubscriptionManager { get; }

        public string TransportAddress => localAddress;
        public string EndpointName => settings.EndpointName();
        public ReadOnlySettings Settings => settings;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }

        public string ToTransportAddress(QueueAddress queueAddress)
        {
            return queueAddress.BaseAddress; // transportInfrastructure.ToTransportAddress(queueAddress);
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

        RawTransportReceiver CreateReceiver()
        {
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return null;
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            var poisonMessageQueue = settings.Get<string>("NServiceBus.Raw.PoisonMessageQueue");



            var receiver = BuildMainReceiver(poisonMessageQueue, purgeOnStartup, GetTransportTransactionMode());

            return receiver;
        }

        TransportTransactionMode GetTransportTransactionMode()
        {
            //TODO: How to get transaction mode
            var transportTransactionSupport = settings.Get<TransportInfrastructure>().TransactionMode;

            //if user haven't asked for a explicit level use what the transport supports
            if (!settings.TryGet(out TransportTransactionMode requestedTransportTransactionMode))
            {
                return transportTransactionSupport;
            }

            if (requestedTransportTransactionMode > transportTransactionSupport)
            {
                throw new Exception($"Requested transaction mode `{requestedTransportTransactionMode}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
            }

            return requestedTransportTransactionMode;
        }

        RawTransportReceiver BuildMainReceiver(string poisonMessageQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport)
        {
            var usePublishSubscribe = false; //TODO: Where to read this from?
            var identifier = Guid.NewGuid().ToString();
            var receiveSettings = new ReceiveSettings(identifier, localAddress, usePublishSubscribe, purgeOnStartup, poisonMessageQueue);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();
            var errorHandlingPolicy = settings.Get<IErrorHandlingPolicy>();
            var receiver = new RawTransportReceiver(messagePump, dispatcher, onMessage, receiveSettings, dequeueLimitations,
                new RawEndpointErrorHandlingPolicy(settings.EndpointName(), localAddress, dispatcher, errorHandlingPolicy));
            return receiver;
        }

        PushRuntimeSettings GetDequeueLimitationsForReceivePipeline()
        {
            if (settings.TryGet("MaxConcurrency", out int concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit);
            }

            return PushRuntimeSettings.Default;
        }

        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        RawCriticalError criticalError;
        IMessageReceiver messagePump;
        IMessageDispatcher dispatcher;
        Func<MessageContext, IMessageDispatcher, Task> onMessage;
        string localAddress;

        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}