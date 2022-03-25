namespace NServiceBus.Raw.DelayedRetries
{
    using Routing;
    using System;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// An error handling policy with immediate and delayed retries.
    /// </summary>
    public class DelayedRetryErrorHandlingPolicy : IErrorHandlingPolicy
    {
        string delayQueue;
        int immediateRetries;
        int delayedRetries;
        string errorQueue;
        TimeSpan delay;

        /// <summary>
        /// Creates new delayed retries policy
        /// </summary>
        /// <param name="immediateRetries">Number of immediate retries.</param>
        /// <param name="delayedRetries">Number of delayed retries.</param>
        /// <param name="delayQueue">Address of the queue used to stored delayed messages.</param>
        /// <param name="errorQueue">Error queue.</param>
        /// <param name="delay">Delay between subsequent delayed retries.</param>
        public DelayedRetryErrorHandlingPolicy(int immediateRetries, int delayedRetries, string delayQueue, string errorQueue, TimeSpan delay)
        {
            this.delayQueue = delayQueue;
            this.errorQueue = errorQueue;
            this.delay = delay;
            this.immediateRetries = immediateRetries;
            this.delayedRetries = delayedRetries;
        }

        /// <summary>
        /// Invoked when an error occurs while processing a message.
        /// </summary>
        /// <param name="handlingContext">Error handling context.</param>
        /// <param name="dispatcher">Dispatcher.</param>
        public async Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher)
        {
            var message = handlingContext.Error.Message;
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetries)
            {
                return ErrorHandleResult.RetryRequired;
            }
            string delayedRetryHeader;
            if (message.Headers.TryGetValue("NServiceBus.Raw.DelayedRetries.Attempt", out delayedRetryHeader))
            {
                var attempt = int.Parse(delayedRetryHeader);
                if (attempt >= delayedRetries)
                {
                    await handlingContext.MoveToErrorQueue(errorQueue).ConfigureAwait(false);
                    return ErrorHandleResult.Handled;
                }
            }

            message.Headers["NServiceBus.Raw.DelayedRetries.Due"] = (DateTime.UtcNow + delay).ToString("O");
            message.Headers["NServiceBus.Raw.DelayedRetries.RetryTo"] = handlingContext.FailedQueue;
            var delayedMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
            var operation = new TransportOperation(delayedMessage, new UnicastAddressTag(delayQueue));
            await dispatcher.Dispatch(new TransportOperations(operation), handlingContext.Error.TransportTransaction)
                .ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}