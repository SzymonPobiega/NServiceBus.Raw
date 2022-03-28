namespace NServiceBus.Raw
{
    using NServiceBus.Transport;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        public static RawEndpointConfiguration CreateSendOnly(string endpointName, TransportDefinition transportDefinition)
        {
            return new RawEndpointConfiguration(endpointName, transportDefinition, null, null);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        public static RawEndpointConfiguration Create(
            string endpointName,
            TransportDefinition transportDefinition,
            Func<MessageContext, IMessageDispatcher, Task> onMessage, //TODO: add cancellation token
            string poisonMessageQueue)
        {
            return new RawEndpointConfiguration(endpointName, transportDefinition, onMessage, poisonMessageQueue);
        }

        RawEndpointConfiguration(
            string endpointName,
            TransportDefinition transportDefinition,
            Func<MessageContext, IMessageDispatcher, Task> onMessage,
            string poisonMessageQueue)
        {
            ValidateEndpointName(endpointName);

            this.endpointName = endpointName;
            this.transportDefinition = transportDefinition;
            this.onMessage = onMessage;
            this.poisonMessageQueue = poisonMessageQueue;
            errorHandlingPolicy = new DefaultErrorHandlingPolicy(poisonMessageQueue, 3);
            sendOnly = onMessage == null;
            pushRuntimeSettings = PushRuntimeSettings.Default;
            criticalErrorAction = (_, __) => Task.CompletedTask;
        }

        /// <summary>
        /// Instructs the endpoint to use a custom error handling policy.
        /// </summary>
        public void CustomErrorHandlingPolicy(IErrorHandlingPolicy customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            errorHandlingPolicy = customPolicy;
        }

        /// <summary>
        /// Sets the number of immediate retries when message processing fails.
        /// </summary>
        public void DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            Guard.AgainstNegative(nameof(immediateRetryCount), immediateRetryCount);
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);

            poisonMessageQueue = errorQueue;
            errorHandlingPolicy = new DefaultErrorHandlingPolicy(errorQueue, immediateRetryCount);
        }

        /// <summary>
        /// Instructs the endpoint to automatically create input queue, poison queue and optionally additional queues if they do not exist.
        /// </summary>
        public void AutoCreateQueues(string[] additionalQueues = null)
        {
            setupInfrastructure = true;

            if (additionalQueues != null)
            {
                this.additionalQueues = additionalQueues;
            }
        }

        /// <summary>
        /// Customizes the behavior should a critical error occur
        /// </summary>
        public void CriticalErrorAction(Func<ICriticalErrorContext, CancellationToken, Task> criticalErrorAction)
        {
            Guard.AgainstNull(nameof(criticalErrorAction), criticalErrorAction);

            this.criticalErrorAction = criticalErrorAction;
        }

        /// <summary>
        /// Instructs the endpoint to not enable the pub/sub capabilities of the transport.
        /// </summary>
        public void DisablePublishAndSubscribe()
        {
            disablePublishAndSubscribe = true;
        }

        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public void LimitMessageProcessingConcurrencyTo(int maxConcurrency)
        {
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

            pushRuntimeSettings = new PushRuntimeSettings(maxConcurrency);
        }

        internal InitializableRawEndpoint Build()
        {
            return new InitializableRawEndpoint(this);
        }

        static void ValidateEndpointName(string endpointName)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            if (endpointName.Contains("@"))
            {
                throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
            }
        }


        //todo: use private set
        internal bool sendOnly;
        internal IErrorHandlingPolicy errorHandlingPolicy;
        internal Func<MessageContext, IMessageDispatcher, Task> onMessage;
        internal string poisonMessageQueue;
        internal string endpointName;
        internal TransportDefinition transportDefinition;
        internal PushRuntimeSettings pushRuntimeSettings;
        internal bool setupInfrastructure;
        internal string[] additionalQueues = new string[0];
        internal bool disablePublishAndSubscribe;
        internal Func<ICriticalErrorContext, CancellationToken, Task> criticalErrorAction;
    }
}