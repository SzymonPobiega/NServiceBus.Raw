namespace NServiceBus.Raw
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        string errorQueue;
        int immediateRetryCount;

        public DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            this.errorQueue = errorQueue;
            this.immediateRetryCount = immediateRetryCount;
        }

        public Task<ErrorHandleResult> OnError(ErrorContext errorContext, IDispatchMessages dispatcher, Func<ErrorContext, string, Task<ErrorHandleResult>> sendToErrorQueue)
        {
            if (errorContext.ImmediateProcessingFailures < immediateRetryCount)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
            return sendToErrorQueue(errorContext, errorQueue);
        }
    }
}