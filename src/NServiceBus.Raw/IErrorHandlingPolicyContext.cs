namespace NServiceBus.Raw
{
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Context for error handling policy.
    /// </summary>
    public interface IErrorHandlingPolicyContext
    {
        /// <summary>
        /// Moves a given message to a specific error queue.
        /// </summary>
        /// <param name="errorQueue">Error queue address.</param>
        /// <param name="attachStandardFailureHeaders">If should include standard error information.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<ErrorHandleResult> MoveToErrorQueue(string errorQueue, bool attachStandardFailureHeaders = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a given message to the configured error queue.
        /// </summary>
        /// <param name="attachStandardFailureHeaders">If should include standard error information.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<ErrorHandleResult> MoveToErrorQueue(bool attachStandardFailureHeaders = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the error information.
        /// </summary>
        ErrorContext Error { get; }

        /// <summary>
        /// The queue from which the failed message has been received.
        /// </summary>
        string FailedQueue { get; }
    }
}