namespace NServiceBus.Raw.DelayedRetries
{
    using System.Threading.Tasks;
    using Transport;

    public class DelayedRetryErrorHandlingPolicy : IErrorHandlingPolicy
    {
        string delayQueue;
        int immediateRetries;
        int delayedRetries;
        string errorQueue;

        public DelayedRetryErrorHandlingPolicy(int immediateRetries, int delayedRetries, string delayQueue, string errorQueue)
        {
            this.delayQueue = delayQueue;
            this.errorQueue = errorQueue;
            this.immediateRetries = immediateRetries;
            this.delayedRetries = delayedRetries;
        }

        public async Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IDispatchMessages dispatcher)
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

            await handlingContext.MoveToErrorQueue(delayQueue).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}