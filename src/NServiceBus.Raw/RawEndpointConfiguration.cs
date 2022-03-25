namespace NServiceBus.Raw
{
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using System;
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
            Func<MessageContext,
            IMessageDispatcher, Task> onMessage,
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
            SendOnly = onMessage == null;
            pushRuntimeSettings = PushRuntimeSettings.Default;
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
        /// Instructs the endpoint to automatically create input queue and poison queue if they do not exist.
        /// </summary>
        public void AutoCreateQueue(string identity = null)
        {
            Settings.Set("NServiceBus.Raw.CreateQueue", true);
            if (identity != null)
            {
                Settings.Set("NServiceBus.Raw.Identity", identity);
            }
        }

        /// <summary>
        /// Instructs the endpoint to automatically create input queue, poison queue and provided additional queues if they do not exist.
        /// </summary>
        public void AutoCreateQueues(string[] additionalQueues, string identity = null)
        {
            foreach (var additionalQueue in additionalQueues)
            {
                queueBindings.BindSending(additionalQueue);
            }
            AutoCreateQueue(identity);
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
            return new InitializableRawEndpoint(Settings, this);
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


        /// <summary>
        /// Exposes raw settings object.
        /// </summary>
        public SettingsHolder Settings { get; } = new SettingsHolder();
        internal bool SendOnly { get; }

        internal IErrorHandlingPolicy errorHandlingPolicy;
        internal Func<MessageContext, IMessageDispatcher, Task> onMessage;
        internal string poisonMessageQueue;
        QueueBindings queueBindings = new QueueBindings();
        internal string endpointName;
        internal TransportDefinition transportDefinition;
        internal PushRuntimeSettings pushRuntimeSettings;
    }
}