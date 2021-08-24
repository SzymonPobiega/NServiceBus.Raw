using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class RawTransportReceiver
    {
        public RawTransportReceiver(IMessageReceiver pushMessages, IMessageDispatcher dispatcher, OnMessage onMessage, string transportAddress, PushRuntimeSettings pushRuntimeSettings, RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.receiver = pushMessages;
            this.dispatcher = dispatcher;
            this.transportAddress = transportAddress;
            this.onMessage = onMessage;
            this.onError = (context, cancellationToken) => errorHandlingPolicy.OnError(context, cancellationToken);
        }

        public Task Initialize(CancellationToken cancellationToken)
        {
            return receiver.Initialize(pushRuntimeSettings, (context, token) => onMessage(context, dispatcher, token), onError, cancellationToken);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver is starting, listening to queue {0}.", transportAddress);

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
        readonly IMessageReceiver receiver;
        readonly IMessageDispatcher dispatcher;
        readonly string transportAddress;
        readonly OnMessage onMessage;
        readonly OnError onError;

        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}