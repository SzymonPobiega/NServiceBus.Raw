namespace NServiceBus.Raw
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class SendOnlyRawEndpointConfiguration
    {
        readonly string endpointName;
        readonly TransportDefinition transportDefinition;
        Action<string, Exception, CancellationToken> criticalErrorAction = (s, exception, arg3) => { };

        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="transport">Transport to use.</param>
        public static SendOnlyRawEndpointConfiguration Create(string endpointName, TransportDefinition transport)
        {
            return new SendOnlyRawEndpointConfiguration(endpointName, transport);
        }

        internal SendOnlyRawEndpointConfiguration(string endpointName, TransportDefinition transport)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            this.endpointName = endpointName;
            this.transportDefinition = transport;
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
            var hostSettings = new HostSettings(endpointName, endpointName, new StartupDiagnosticEntries(), CriticalErrorAction, false);

            var transportInfrastructure = await transportDefinition.Initialize(hostSettings, new ReceiveSettings[0], new string[0])
                .ConfigureAwait(false);

            var startableEndpoint = new StartableRawEndpoint(transportDefinition, transportInfrastructure, null, endpointName, null);
            return startableEndpoint;
        }
    }
}