namespace NServiceBus.Raw.DelayedRetries
{
    using System;
    using System.Threading.Tasks;
    using Faults;
    using Routing;
    using Transport;

    public class DelayedRetryEndpoint<T>
        where T : TransportDefinition, new()
    {
        TimeSpan delayBy;
        string storageQueueName;
        string inputQueueName;
        Action<TransportExtensions<T>> transportCustomization;
        string poisonMessageQueue;
        IReceivingRawEndpoint outEndpoint;
        IReceivingRawEndpoint inEndpoint;

        public DelayedRetryEndpoint(string storageQueueName, string inputQueueName, TimeSpan delayBy, string poisonMessageQueue = null, Action<TransportExtensions<T>> transportCustomization = null)
        {
            this.delayBy = delayBy;
            this.storageQueueName = storageQueueName;
            this.inputQueueName = inputQueueName;
            this.transportCustomization = transportCustomization ?? EmptyTransportCustomization;
            this.poisonMessageQueue = poisonMessageQueue ?? "poison";
        }

        public async Task Start()
        {
            var outConfig = RawEndpointConfiguration.Create(storageQueueName, OnOutgoingMessage, poisonMessageQueue);
            outConfig.LimitMessageProcessingConcurrencyTo(1);
            transportCustomization(outConfig.UseTransport<T>());
            outConfig.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            outConfig.AutoCreateQueue();

            var inConfig = RawEndpointConfiguration.Create(inputQueueName, OnIncomingMessage, poisonMessageQueue);
            transportCustomization(inConfig.UseTransport<T>());
            inConfig.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            inConfig.AutoCreateQueue();

            outEndpoint = await RawEndpoint.Start(outConfig).ConfigureAwait(false);
            inEndpoint = await RawEndpoint.Start(inConfig).ConfigureAwait(false);
        }

        Task OnIncomingMessage(MessageContext incomingMessage, IDispatchMessages dispatcher)
        {
            incomingMessage.Headers["NServiceBus.Raw.DelayedRetries.Due"] = (DateTime.UtcNow + delayBy).ToString("O");

            var message = new OutgoingMessage(incomingMessage.MessageId, incomingMessage.Headers, incomingMessage.Body);
            var operation = new TransportOperation(message, new UnicastAddressTag(storageQueueName));
            return dispatcher.Dispatch(new TransportOperations(operation), incomingMessage.TransportTransaction, incomingMessage.Extensions);
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
                || !headers.TryGetValue(FaultsHeaderKeys.FailedQ, out destination))
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
            headers.Remove(FaultsHeaderKeys.FailedQ);
            headers["NServiceBus.Raw.DelayedRetries.Attempt"] = attempt.ToString();

            var message = new OutgoingMessage(delayedMessage.MessageId, headers, delayedMessage.Body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));
            await dispatcher.Dispatch(new TransportOperations(operation), delayedMessage.TransportTransaction, delayedMessage.Extensions).ConfigureAwait(false);
        }

        public Task Stop()
        {
            return Task.WhenAll(outEndpoint.Stop(), inEndpoint.Stop());
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
