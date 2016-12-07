using System;
using System.Reflection;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Security.Principal;

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(SettingsHolder settings, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            this.settings = settings;
            this.onMessage = onMessage;
        }

        public async Task<IStartableRawEndpoint> Initialize()
        {
            CreateCriticalErrorHandler();

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = GetConnectionString(transportDefinition);
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            var sendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
            var dispatcher = sendInfrastructure.DispatcherFactory();

            IPushMessages messagePump = null;
            if (!settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                var receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
                messagePump = receiveInfrastructure.MessagePumpFactory();

                var queueCreator = receiveInfrastructure.QueueCreatorFactory();

                var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? settings.EndpointName();

                var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(settings.EndpointName()));

                var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
                settings.SetDefault<LogicalAddress>(mainLogicalAddress);

                var mainAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress);
                settings.SetDefault("NServiceBus.SharedQueue", mainAddress);

                if (settings.GetOrDefault<bool>("NServiceBus.Raw.CreateQueue"))
                {
                    var bindings = new QueueBindings();
                    bindings.BindReceiving(mainAddress);
                    bindings.BindReceiving(settings.Get<string>("NServiceBus.Raw.PoisonMessageQueue"));
                    await queueCreator.CreateQueueIfNecessary(bindings, GetInstallationUserName()).ConfigureAwait(false);
                }
            }

            var startableEndpoint = new StartableRawEndpoint(settings, transportInfrastructure, CreateCriticalErrorHandler(), messagePump, dispatcher, onMessage);
            return startableEndpoint;
        }

        string GetInstallationUserName()
        {
            string username;
            return settings.TryGet("NServiceBus.Raw.Identity", out username)
                ? username
                : WindowsIdentity.GetCurrent().Name;
        }

        string GetConnectionString(TransportDefinition transportDefinition)
        {
            var instance = settings.Get(connectionStringType.FullName);
            return (string) connectionStringGetter.Invoke(instance, new object[] {transportDefinition});
        }

        RawCriticalError CreateCriticalErrorHandler()
        {
            Func<ICriticalErrorContext, Task> errorAction;
            settings.TryGet("onCriticalErrorAction", out errorAction);
            return new RawCriticalError(errorAction);
        }

        SettingsHolder settings;
        Func<MessageContext, IDispatchMessages, Task> onMessage;

        static Type connectionStringType = typeof(IEndpointInstance).Assembly.GetType("NServiceBus.TransportConnectionString", true);
        static MethodInfo connectionStringGetter = connectionStringType.GetMethod("GetConnectionStringOrRaiseError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
}