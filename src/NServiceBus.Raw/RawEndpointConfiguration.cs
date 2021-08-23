using System;
using System.Reflection;
using System.Threading.Tasks;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;
    using Serialization;
    using Unicast.Messages;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        readonly string endpointName;
        readonly string queueName;
        readonly TransportDefinition transportDefinition;
        readonly OnMessage onMessage;
        readonly string errorQueue;
        IErrorHandlingPolicy errorHandlingPolicy;
        int concurrencyLimit;
        Action<string, Exception, CancellationToken> criticalErrorAction = (s, exception, arg3) => { };

        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="transport">Transport to use.</param>
        public static SendOnlyRawEndpointConfiguration CreateSendOnly(string endpointName, TransportDefinition transport)
        {
            return new SendOnlyRawEndpointConfiguration(endpointName, transport);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="queueName">The address of the queue to receive from.</param>
        /// <param name="transport">Transport to use.</param>
        /// <param name="onMessage">Callback invoked when a message is received.</param>
        /// <param name="errorQueue">Queue to move messages that fail to be received or processed.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration Create(string endpointName, string queueName, TransportDefinition transport, OnMessage onMessage, string errorQueue)
        {
            return new RawEndpointConfiguration(endpointName, queueName, transport, onMessage, errorQueue);
        }

        RawEndpointConfiguration(string endpointName, string queueName, TransportDefinition transport, OnMessage onMessage, string errorQueue)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            this.endpointName = endpointName;
            this.queueName = queueName;
            this.transportDefinition = transport;
            this.onMessage = onMessage;
            this.errorQueue = errorQueue;

            ErrorHandlingPolicy = new DefaultErrorHandlingPolicy(5);
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
            var ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Func<Type, bool>) }, null);
#pragma warning disable CS0618 // Type or member is obsolete
            settings.SetDefault<MessageMetadataRegistry>(() => ctor.Invoke(new object[] { (Func<Type, bool>)isMessageType }));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// The error handling policy to use.
        /// </summary>
        public IErrorHandlingPolicy ErrorHandlingPolicy
        {
            get => errorHandlingPolicy;
            set
            {
                Guard.AgainstNull(nameof(value), value);
                errorHandlingPolicy = value;
            }
        }

        /// <summary>
        /// Configures if the endpoint should attempt to set up the infrastructure (e.g. queues and topics) before starting.
        /// </summary>
        public InfrastructureSetup InfrastructureSetup { get; set; }

        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        public int ConcurrencyLimit
        {
            get => concurrencyLimit;
            set
            {
                Guard.AgainstNegativeAndZero(nameof(value), value);
                concurrencyLimit = value;
            }
        }

        /// <summary>
        /// Action to invoke when the receiver detects a critical error.
        /// </summary>
        public Action<string, Exception, CancellationToken> CriticalErrorAction
        {
            get => criticalErrorAction;
            set
            {
                Guard.AgainstNull(nameof(value), value);
                criticalErrorAction = value;
            }
        }

        internal async Task<IStartableRawEndpoint> Initialize()
        {
            var hostSettings = new HostSettings(endpointName, endpointName, new StartupDiagnosticEntries(), CriticalErrorAction, InfrastructureSetup.Create);

            var receiveSettings = new ReceiveSettings("Main", queueName, true, false, errorQueue);

            var transportInfrastructure = await transportDefinition.Initialize(hostSettings, new[] { receiveSettings }, InfrastructureSetup.AdditionalQueues ?? new string[0])
                .ConfigureAwait(false);

            var pump = transportInfrastructure.Receivers[receiveSettings.Id];

            var receiver = new RawTransportReceiver(pump, transportInfrastructure.Dispatcher, onMessage, queueName, new PushRuntimeSettings(concurrencyLimit),
                new RawEndpointErrorHandlingPolicy(errorQueue, endpointName, queueName, transportInfrastructure.Dispatcher, errorHandlingPolicy));

            var startableEndpoint = new StartableRawEndpoint(transportDefinition, transportInfrastructure, receiver, endpointName, queueName);
            return startableEndpoint;
        }
    }
}