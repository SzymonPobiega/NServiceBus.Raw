namespace NServiceBus.Raw.DelayedRetries
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class DelayedRetryErrorHandlingPolicy : IErrorHandlingPolicy
    {
        TimeSpan delayBy;
        string delayQueue;
        int immediateRetries;
        int delayedRetries;
        string errorQueue;

        public DelayedRetryErrorHandlingPolicy(int immediateRetries, int delayedRetries, TimeSpan delayBy, string delayQueue, string errorQueue)
        {
            this.delayQueue = delayQueue;
            this.delayBy = delayBy;
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

            message.Headers["NServiceBus.Raw.DelayedRetries.Due"] = (DateTime.UtcNow + delayBy).ToString("O");
            await handlingContext.MoveToErrorQueue(delayQueue).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}