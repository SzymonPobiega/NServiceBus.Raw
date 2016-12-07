namespace NServiceBus.Raw
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Represents a policy for handling errors.
    /// </summary>
    public interface IErrorHandlingPolicy
    {
        /// <summary>
        /// Invoked when an error occurs while processing a message.
        /// </summary>
        /// <param name="errorContext">Error context.</param>
        /// <param name="dispatcher">Dispatcher.</param>
        /// <param name="sendToErrorQueue">Instructs the underlying infrastructure to send the message to the error queue.</param>
        Task<ErrorHandleResult> OnError(ErrorContext errorContext, IDispatchMessages dispatcher, Func<ErrorContext, string, Task<ErrorHandleResult>> sendToErrorQueue);
    }
}