namespace NServiceBus.Raw.DelayedRetries
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Transport;

    /// <summary>
    /// An endpoint that can delay retry messages.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelayedRetryEndpoint<T>
        where T : TransportDefinition, new()
    {
        string storageQueueName;
        Action<TransportExtensions<T>> transportCustomization;
        string poisonMessageQueue;
        IReceivingRawEndpoint outEndpoint;

        /// <summary>
        /// Creates new instance of a delay retry endpoint.
        /// </summary>
        /// <param name="storageQueueName">Address of a queue to be used to store delayed retry messages.</param>
        /// <param name="poisonMessageQueue">Address of a poison message queue.</param>
        /// <param name="transportCustomization">A callback for customizing the transport.</param>
        public DelayedRetryEndpoint(string storageQueueName, string poisonMessageQueue = null, Action<TransportExtensions<T>> transportCustomization = null)
        {
            this.storageQueueName = storageQueueName;
            this.transportCustomization = transportCustomization ?? EmptyTransportCustomization;
            this.poisonMessageQueue = poisonMessageQueue ?? "poison";
        }

        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        public async Task Start()
        {
            var outConfig = RawEndpointConfiguration.Create(storageQueueName, OnOutgoingMessage, poisonMessageQueue);
            outConfig.LimitMessageProcessingConcurrencyTo(1);
            transportCustomization(outConfig.UseTransport<T>());
            outConfig.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            outConfig.AutoCreateQueue();

            outEndpoint = await RawEndpoint.Start(outConfig).ConfigureAwait(false);
        }

        static void EmptyTransportCustomization(TransportExtensions<T> transport)
        {
        }

        static async Task OnOutgoingMessage(MessageContext delayedMessage, IDispatchMessages dispatcher)
        {
            string dueHeader;
            string destination;
            var headers = delayedMessage.Headers;
            if (!headers.TryGetValue("NServiceBus.Raw.DelayedRetries.Due", out dueHeader)
                || !headers.TryGetValue("NServiceBus.Raw.DelayedRetries.RetryTo", out destination))
            {
                //Skip
                return;
            }
            var due = DateTime.Parse(dueHeader).ToUniversalTime();
            var sleepTime = due - DateTime.UtcNow;
            if (sleepTime > TimeSpan.Zero)
            {
                await Task.Delay(sleepTime).ConfigureAwait(false);
            }

            var attempt = 1;
            string delayedRetryHeader;
            if (delayedMessage.Headers.TryGetValue("NServiceBus.Raw.DelayedRetries.Attempt", out delayedRetryHeader))
            {
                attempt = int.Parse(delayedRetryHeader);
            }
            attempt++;
            headers.Remove("NServiceBus.Raw.DelayedRetries.Due");
            headers.Remove("NServiceBus.Raw.DelayedRetries.RetryTo");
            headers["NServiceBus.Raw.DelayedRetries.Attempt"] = attempt.ToString();

            var message = new OutgoingMessage(delayedMessage.MessageId, headers, delayedMessage.Body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));
            await dispatcher.Dispatch(new TransportOperations(operation), delayedMessage.TransportTransaction, delayedMessage.Extensions).ConfigureAwait(false);
        }

        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        /// <returns></returns>
        public Task Stop()
        {
            return outEndpoint.Stop();
        }

        class RetryForeverPolicy : IErrorHandlingPolicy
        {
            public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IDispatchMessages dispatcher)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
        }
    }
}
