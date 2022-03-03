using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using Serialization;
    using Unicast.Messages;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        Func<MessageContext, IDispatchMessages, Task> onMessage;
        QueueBindings queueBindings;

        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration CreateSendOnly(string endpointName)
        {
            return new RawEndpointConfiguration(endpointName, null, null);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <param name="onMessage">Callback invoked when a message is received.</param>
        /// <param name="poisonMessageQueue">Queue to move poison messages that can't be received from transport.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration Create(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage, string poisonMessageQueue)
        {
            return new RawEndpointConfiguration(endpointName, onMessage, poisonMessageQueue);
        }

        RawEndpointConfiguration(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage, string poisonMessageQueue)
        {
            this.onMessage = onMessage;
            ValidateEndpointName(endpointName);

            var sendOnly = onMessage == null;
            Settings.Set("Endpoint.SendOnly", sendOnly);
            Settings.Set("TypesToScan", new Type[0]);
            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            Settings.PrepareConnectionString();
            queueBindings = Settings.Get<QueueBindings>();

            if (!sendOnly)
            {
                queueBindings.BindSending(poisonMessageQueue);
                Settings.Set("NServiceBus.Raw.PoisonMessageQueue", poisonMessageQueue);
                Settings.SetDefault<IErrorHandlingPolicy>(new DefaultErrorHandlingPolicy(poisonMessageQueue, 5));
            }

            SetTransportSpecificFlags(Settings, poisonMessageQueue);
        }

        static void SetTransportSpecificFlags(SettingsHolder settings, string poisonQueue)
        {
            //To satisfy requirements of various transports

            //MSMQ
            settings.Set("errorQueue", poisonQueue); //Not SetDefault Because MSMQ transport verifies if that value has been explicitly set

            //RabbitMQ
            settings.SetDefault("RabbitMQ.RoutingTopologySupportsDelayedDelivery", true);

            //SQS
            settings.SetDefault("NServiceBus.AmazonSQS.DisableSubscribeBatchingOnStart", true);

            //ASB
            var builder = new ConventionsBuilder(settings);
            builder.DefiningEventsAs(type => true);
            settings.Set(builder.Conventions);

            //ASQ and ASB
            var serializer = Tuple.Create(new NewtonsoftSerializer() as SerializationDefinition, new SettingsHolder());
            settings.SetDefault("MainSerializer", serializer);

            //SQS and ASQ
            bool isMessageType(Type t) => true;            
            var registry = CreateMessageMetadataRegistry();
#pragma warning disable CS0618 // Type or member is obsolete
            settings.SetDefault(registry);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        static MessageMetadataRegistry CreateMessageMetadataRegistry()
        {
            // Ctor for NServiceBus <= 7.6
            var ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Func<Type, bool>) }, null);
            if (ctor != null)
            {
                return (MessageMetadataRegistry)ctor.Invoke(new object[] { (Func<Type, bool>)isMessageType });
            }

            // Ctor for NServiceBus 7.7, adds `bool allowDynamicTypeLoading`, default to true to keep same behavior
            ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Func<Type, bool>), bool }, null);
            return (MessageMetadataRegistry)ctor.Invoke(new object[] { (Func<Type, bool>)isMessageType, true });
        }

        /// <summary>
        /// Exposes raw settings object.
        /// </summary>
        public SettingsHolder Settings { get; } = new SettingsHolder();

        /// <summary>
        /// Instructs the endpoint to use a custom error handling policy.
        /// </summary>
        public void CustomErrorHandlingPolicy(IErrorHandlingPolicy customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            Settings.Set(customPolicy);
        }

        /// <summary>
        /// Sets the number of immediate retries when message processing fails.
        /// </summary>
        public void DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            Guard.AgainstNegative(nameof(immediateRetryCount), immediateRetryCount);
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);
            Settings.Set<IErrorHandlingPolicy>(new DefaultErrorHandlingPolicy(errorQueue, immediateRetryCount));
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

            Settings.Set("MaxConcurrency", maxConcurrency);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public TransportExtensions<T> UseTransport<T>() where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var transportDefinition = new T();
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, Settings);

            ConfigureTransport(transportDefinition);
            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public TransportExtensions UseTransport(Type transportDefinitionType)
        {
            Guard.AgainstNull(nameof(transportDefinitionType), transportDefinitionType);
            Guard.TypeHasDefaultConstructor(transportDefinitionType, nameof(transportDefinitionType));

            var transportDefinition = Construct<TransportDefinition>(transportDefinitionType);
            ConfigureTransport(transportDefinition);
            return new TransportExtensions(Settings);
        }

        void ConfigureTransport(TransportDefinition transportDefinition)
        {
            Settings.Set(transportDefinition);
        }

        static T Construct<T>(Type type)
        {
            var defaultConstructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
            {
            }, null);
            if (defaultConstructor != null)
            {
                return (T)defaultConstructor.Invoke(null);
            }

            return (T)Activator.CreateInstance(type);
        }

        internal InitializableRawEndpoint Build()
        {
            return new InitializableRawEndpoint(Settings, onMessage);
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
    }
}