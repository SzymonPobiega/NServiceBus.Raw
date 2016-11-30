using System;
using System.Threading.Tasks;
using NServiceBus.ConsistencyGuarantees;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
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

        public async Task<IRawEndpointInstance> Start()
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
            var errorQueue = settings.ErrorQueueAddress();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var receiver = BuildMainReceiver(errorQueue, purgeOnStartup, requiredTransactionSupport);

            return receiver;
        }

        RawTransportReceiver BuildMainReceiver(string errorQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport)
        {
            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();
            var errorHandlingPolicy = new RawEndpointErrorHandlingPolicy(settings, dispatcher, errorQueue, GetImmediateRetryCount());

            var receiver = new RawTransportReceiver(messagePump, dispatcher, onMessage, pushSettings, dequeueLimitations, criticalError, errorHandlingPolicy);
            return receiver;
        }

        int GetImmediateRetryCount()
        {
            int retryCount;
            return settings.TryGet("RetryCount", out retryCount) 
                ? retryCount 
                : 5;
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