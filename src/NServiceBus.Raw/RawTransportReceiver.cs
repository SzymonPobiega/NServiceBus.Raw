using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    class RawTransportReceiver
    {
        public RawTransportReceiver(Func<IPushMessages> pushMessages, IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> onMessage, PushSettings pushSettings, PushRuntimeSettings pushRuntimeSettings, CriticalError criticalError, RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.criticalError = criticalError;
            this.errorHandlingPolicy = errorHandlingPolicy;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pushSettings = pushSettings;

            pumpFactory = pushMessages;
            this.onMessage = context => onMessage(context, dispatcher);
        }

        public Task Init()
        {
            receiver = pumpFactory();
            return receiver.Init(ctx => onMessage(ctx), ctx => errorHandlingPolicy.OnError(ctx), criticalError, pushSettings);
        }

        public async Task Start()
        {
            if (isStarted)
            {
                return;
            }

            try
            {
                Logger.DebugFormat("Receiver is starting, listening to queue {0}.", pushSettings.InputQueue);

                if (receiver == null)
                {
                    await Init().ConfigureAwait(false);
                }

                receiver.Start(pushRuntimeSettings);

                isStarted = true;
            }
            catch
            {
                //We call methods that can fail on a local variable to make sure the state is clean after Start fails and we can re-try
                var receiverStopping = receiver;
                receiver = null;
                isStarted = false;

                if (receiverStopping != null)
                {
                    await receiverStopping.Stop().ConfigureAwait(false);
                }
                throw;
            }
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            //We call methods that can fail on a local variable to make sure the state is clean after Stop fails and we can restart
            var receiverStopping = receiver;
            receiver = null;
            isStarted = false;

            await receiverStopping.Stop().ConfigureAwait(false);
            if (receiverStopping is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        CriticalError criticalError;
        RawEndpointErrorHandlingPolicy errorHandlingPolicy;
        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;
        IPushMessages receiver;
        Func<IPushMessages> pumpFactory;
        Func<MessageContext, Task> onMessage;

        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}