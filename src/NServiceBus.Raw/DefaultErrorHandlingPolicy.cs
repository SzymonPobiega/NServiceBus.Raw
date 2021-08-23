namespace NServiceBus.Raw
{
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Default error handling policy.
    /// </summary>
    public class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        int immediateRetryCount;

        /// <summary>
        /// Creates a new instance of the default error handling policy with configurable number of immediate retries.
        /// </summary>
        /// <param name="immediateRetryCount"></param>
        public DefaultErrorHandlingPolicy(int immediateRetryCount)
        {
            this.immediateRetryCount = immediateRetryCount;
        }

        /// <inheritdoc />
        public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IMessageDispatcher dispatcher)
        {
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetryCount)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
            return handlingContext.MoveToErrorQueue();
        }
    }
}