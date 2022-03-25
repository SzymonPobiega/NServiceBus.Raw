using NServiceBus.Logging;
using NServiceBus.Transport;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    class RawTransportReceiver
    {
        public RawTransportReceiver(
            IMessageReceiver pushMessages,
            IMessageDispatcher dispatcher,
            Func<MessageContext, IMessageDispatcher, Task> onMessage,
            PushRuntimeSettings pushRuntimeSettings,
            RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.errorHandlingPolicy = errorHandlingPolicy;
            this.pushRuntimeSettings = pushRuntimeSettings;
            Receiver = pushMessages;
            this.onMessage = ctx => onMessage(ctx, dispatcher);
        }

        public IMessageReceiver Receiver;

        public Task Init()
        {
            return Receiver.Initialize(pushRuntimeSettings, (ctx, _) => onMessage(ctx), (ctx, _) => errorHandlingPolicy.OnError(ctx));
        }

        public async void Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            try
            {
                Logger.DebugFormat("Receiver is starting, listening to queue {0}.", Receiver.ReceiveAddress);

                await Receiver.StartReceive();

                isStarted = true;
            }
            catch
            {
                isStarted = false;
                await Receiver.StopReceive();
                throw;
            }
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            await Receiver.StopReceive().ConfigureAwait(false);
            if (Receiver is IDisposable disposable)
            {
                disposable.Dispose();
            }
            isStarted = false;
        }

        RawEndpointErrorHandlingPolicy errorHandlingPolicy;
        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        private readonly Func<MessageContext, Task> onMessage;
        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}