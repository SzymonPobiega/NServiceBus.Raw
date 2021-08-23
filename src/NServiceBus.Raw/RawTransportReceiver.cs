using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class RawTransportReceiver
    {
        public RawTransportReceiver(IMessageReceiver pushMessages, IMessageDispatcher dispatcher, Func<MessageContext, IMessageDispatcher, Task> onMessage, ReceiveSettings receiveSettings, PushRuntimeSettings pushRuntimeSettings, RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.receiveSettings = receiveSettings;
            this.receiver = pushMessages;
            this.onMessage = (context, cancellationToken) => onMessage(context, dispatcher);
            this.onError = (context, cancellationToken) => errorHandlingPolicy.OnError(context, cancellationToken);
        }

        public Task Initialize(CancellationToken cancellationToken)
        {
            return receiver.Initialize(pushRuntimeSettings, onMessage, onError, cancellationToken);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver is starting, listening to queue {0}.", receiveSettings.ReceiveAddress);

            receiver.StartReceive(cancellationToken);

            isStarted = true;
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            if (!isStarted)
            {
                return;
            }

            await receiver.StopReceive(cancellationToken).ConfigureAwait(false);
            if (receiver is IDisposable disposable)
            {
                disposable.Dispose();
            }
            isStarted = false;
        }

        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        ReceiveSettings receiveSettings;
        IMessageReceiver receiver;
        OnMessage onMessage;
        OnError onError;

        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}