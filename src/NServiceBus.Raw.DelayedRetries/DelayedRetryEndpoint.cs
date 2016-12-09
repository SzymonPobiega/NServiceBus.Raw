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
        string delayQueueName;
        Action<TransportExtensions<T>> transportCustomization;
        string poisonMessageQueue;
        IRawEndpointInstance endpoint;

        public DelayedRetryEndpoint(string delayQueueName, TimeSpan delayBy, string poisonMessageQueue = null, Action<TransportExtensions<T>> transportCustomization = null)
        {
            this.delayBy = delayBy;
            this.delayQueueName = delayQueueName;
            this.transportCustomization = transportCustomization ?? EmptyTransportCustomization;
            this.poisonMessageQueue = poisonMessageQueue ?? "poison";
        }

        public IErrorHandlingPolicy CreatePolicyObject(int immediateRetries, int delayedRetries, string errorQueue)
        {
            return new DelayedRetryErrorHandlingPolicy(immediateRetries, delayedRetries, delayBy, delayQueueName, errorQueue);
        }

        public async Task Start()
        {
            var config = RawEndpointConfiguration.Create(delayQueueName, OnMessage, poisonMessageQueue);
            config.LimitMessageProcessingConcurrencyTo(1);
            var transport = config.UseTransport<T>();
            transportCustomization(transport);
            config.CustomErrorHandlingPolicy(new RetryForeverPolicy());
            config.AutoCreateQueue();
            endpoint = await RawEndpoint.Start(config).ConfigureAwait(false);
        }

        static void EmptyTransportCustomization(TransportExtensions<T> transport)
        {
        }

        static async Task OnMessage(MessageContext delayedMessage, IDispatchMessages dispatcher)
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
            await dispatcher.Dispatch(new TransportOperations(operation), delayedMessage.TransportTransaction, delayedMessage.Context);
        }

        public Task Stop()
        {
            return endpoint.Stop();
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
