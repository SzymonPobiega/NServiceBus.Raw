using System;
using System.Reflection;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(SettingsHolder settings, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            this.settings = settings;
            this.onMessage = onMessage;
        }

        public Task<IStartableRawEndpoint> Initialize()
        {
            CreateCriticalErrorHandler();

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = GetConnectionString();
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            var sendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
            var dispatcher = sendInfrastructure.DispatcherFactory();

            IPushMessages messagePump = null;
            if (!settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                var receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
                messagePump = receiveInfrastructure.MessagePumpFactory();

                var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? settings.EndpointName();

                var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(settings.EndpointName()));

                var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
                settings.SetDefault<LogicalAddress>(mainLogicalAddress);

                var mainAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress);
                settings.SetDefault("NServiceBus.SharedQueue", mainAddress);
            }

            var startableEndpoint = new StartableRawEndpoint(settings, transportInfrastructure, CreateCriticalErrorHandler(), messagePump, dispatcher, onMessage);
            return Task.FromResult<IStartableRawEndpoint>(startableEndpoint);
        }

        string GetConnectionString()
        {
            var instance = settings.Get(connectionStringType.FullName);
            return (string) connectionStringGetter.Invoke(instance, new object[0]);
        }

        RawCriticalError CreateCriticalErrorHandler()
        {
            Func<ICriticalErrorContext, Task> errorAction;
            settings.TryGet("onCriticalErrorAction", out errorAction);
            return new RawCriticalError(errorAction);
        }

        SettingsHolder settings;
        Func<MessageContext, IDispatchMessages, Task> onMessage;

        static Type connectionStringType = Type.GetType("NServiceBus.TransportConnectionString", true);
        static MethodInfo connectionStringGetter = connectionStringType.GetMethod("GetConnectionStringOrRaiseError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
}