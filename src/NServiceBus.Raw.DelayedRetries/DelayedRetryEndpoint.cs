namespace NServiceBus.Raw.DelayedRetries
{
    using Routing;
    using System;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// An endpoint that can delay retry messages.
    /// </summary>
    public class DelayedRetryEndpoint
    {
        string storageQueueName;
        TransportDefinition transportDefinition;
        string poisonMessageQueue;
        IReceivingRawEndpoint outEndpoint;

        /// <summary>
        /// Creates new instance of a delay retry endpoint.
        /// </summary>
        /// <param name="storageQueueName">Address of a queue to be used to store delayed retry messages.</param>
        /// <param name="poisonMessageQueue">Address of a poison message queue.</param>
        /// <param name="transportDefinition">The transport configuration.</param>
        public DelayedRetryEndpoint(TransportDefinition transportDefinition, string storageQueueName, string poisonMessageQueue = null)
        {
            this.storageQueueName = storageQueueName;
            this.transportDefinition = transportDefinition;
            this.poisonMessageQueue = poisonMessageQueue ?? "poison";
        }

        /// <summary>
        /// Starts the endpoint.
        /// </summary>
        public async Task Start()
        {
            var outConfig = RawEndpointConfiguration.Create(storageQueueName, transportDefinition, OnOutgoingMessage, poisonMessageQueue);

            outConfig.LimitMessageProcessingConcurrencyTo(1);
            outConfig.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            outConfig.AutoCreateQueues();

            outEndpoint = await RawEndpoint.Start(outConfig).ConfigureAwait(false);
        }

        static async Task OnOutgoingMessage(MessageContext delayedMessage, IMessageDispatcher dispatcher)
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

            var message = new OutgoingMessage(delayedMessage.NativeMessageId, headers, delayedMessage.Body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));
            await dispatcher.Dispatch(new TransportOperations(operation), delayedMessage.TransportTransaction).ConfigureAwait(false);
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
            public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
        }
    }
}
