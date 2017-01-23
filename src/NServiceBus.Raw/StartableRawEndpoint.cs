using System;
using System.Threading.Tasks;
using NServiceBus.ConsistencyGuarantees;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using Extensibility;

    class StartableRawEndpoint : IStartableRawEndpoint
    {
        public StartableRawEndpoint(SettingsHolder settings, TransportInfrastructure transportInfrastructure, RawCriticalError criticalError, IPushMessages messagePump, IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            this.criticalError = criticalError;
            this.messagePump = messagePump;
            this.dispatcher = dispatcher;
            this.onMessage = onMessage;
            this.settings = settings;
            this.transportInfrastructure = transportInfrastructure;
        }

        public async Task<IReceivingRawEndpoint> Start()
        {
            await transportInfrastructure.Start().ConfigureAwait(false);

            var receiver = CreateReceiver();

            if (receiver != null)
            {
                await InitializeReceiver(receiver).ConfigureAwait(false);
            }

            var runningInstance = new RunningRawEndpointInstance(settings, receiver, transportInfrastructure, dispatcher);
            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            if (receiver != null)
            {
                StartReceiver(receiver);
            }
            return runningInstance;
        }

        public string TransportAddress => settings.LocalAddress();
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

        RawTransportReceiver CreateReceiver()
        {
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return null;
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            var poisonMessageQueue = settings.Get<string>("NServiceBus.Raw.PoisonMessageQueue");
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var receiver = BuildMainReceiver(poisonMessageQueue, purgeOnStartup, requiredTransactionSupport);

            return receiver;
        }

        RawTransportReceiver BuildMainReceiver(string poisonMessageQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport)
        {
            var pushSettings = new PushSettings(settings.LocalAddress(), poisonMessageQueue, purgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();
            var errorHandlingPolicy = settings.Get<IErrorHandlingPolicy>();
            var receiver = new RawTransportReceiver(messagePump, dispatcher, onMessage, pushSettings, dequeueLimitations, criticalError, 
                new RawEndpointErrorHandlingPolicy(settings, dispatcher, errorHandlingPolicy));
            return receiver;
        }

        PushRuntimeSettings GetDequeueLimitationsForReceivePipeline()
        {
            int concurrencyLimit;
            if (settings.TryGet("MaxConcurrency", out concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit);
            }

            return PushRuntimeSettings.Default;
        }
        
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        RawCriticalError criticalError;
        IPushMessages messagePump;
        IDispatchMessages dispatcher;
        Func<MessageContext, IDispatchMessages, Task> onMessage;

        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}