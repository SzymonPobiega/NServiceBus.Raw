using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using Extensibility;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(SettingsHolder settings, TransportInfrastructure transportInfrastructure, RawCriticalError criticalError, Func<IPushMessages> messagePump, IDispatchMessages dispatcher, IManageSubscriptions subscriptionManager, Func<MessageContext, IDispatchMessages, Task> onMessage, string localAddress)
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

        public async Task<IReceivingRawEndpoint> Start()
        {
            if(startHasBeenCalled)
            {
                throw new InvalidOperationException("Multiple calls to Start is not supported.");
            }

            startHasBeenCalled = true;

            var receiver = CreateReceiver();

            if (receiver != null)
            {
                await InitializeReceiver(receiver).ConfigureAwait(false);
            }

            var runningInstance = new RunningRawEndpointInstance(settings, receiver, transportInfrastructure, dispatcher, SubscriptionManager, localAddress);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            try
            {
                if (receiver != null)
                {
                    await StartReceiver(receiver).ConfigureAwait(false);
                }

                return runningInstance;
            }
            catch
            {
                await runningInstance.Stop();
               
                throw;
            }
        }

        public IManageSubscriptions SubscriptionManager { get; }

        public string TransportAddress => localAddress;
        public string EndpointName => settings.EndpointName();
        public ReadOnlySettings Settings => settings;

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, context);
        }

        public string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        static Task StartReceiver(RawTransportReceiver receiver)
        {
            try
            {
                return receiver.Start();
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
            var pushSettings = new PushSettings(localAddress, poisonMessageQueue, purgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();
            var errorHandlingPolicy = settings.Get<IErrorHandlingPolicy>();
            var receiver = new RawTransportReceiver(messagePump, dispatcher, onMessage, pushSettings, dequeueLimitations, criticalError,
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
        Func<IPushMessages> messagePump;
        IDispatchMessages dispatcher;
        Func<MessageContext, IDispatchMessages, Task> onMessage;
        string localAddress;
        bool startHasBeenCalled;
        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}