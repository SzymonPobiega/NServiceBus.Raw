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

        public async Task<IStartableRawEndpoint> Initialize()
        {
            CreateCriticalErrorHandler();

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = GetConnectionString(transportDefinition);
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(settings.EndpointName()));
            var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? settings.EndpointName();
            var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
            var localAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress);
            settings.SetDefault<LogicalAddress>(mainLogicalAddress);

            var sendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
            var dispatcher = sendInfrastructure.DispatcherFactory();

            IPushMessages messagePump = null;
            if (!settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                var receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
                var queueCreator = receiveInfrastructure.QueueCreatorFactory();
                messagePump = receiveInfrastructure.MessagePumpFactory();

                if (settings.GetOrDefault<bool>("NServiceBus.Raw.CreateQueue"))
                {
                    var bindings = new QueueBindings();
                    bindings.BindReceiving(localAddress);
                    bindings.BindReceiving(settings.Get<string>("NServiceBus.Raw.PoisonMessageQueue"));
                    await queueCreator.CreateQueueIfNecessary(bindings, GetInstallationUserName()).ConfigureAwait(false);
                }

                RegisterReceivingComponent(settings, localAddress);
            }

            IManageSubscriptions subscriptionManager = null;
            if (transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast ||
                transportInfrastructure.OutboundRoutingPolicy.Sends == OutboundRoutingType.Multicast)
            {
                subscriptionManager = CreateSubscriptionManager(transportInfrastructure);
            }

            var startableEndpoint = new StartableRawEndpoint(settings, transportInfrastructure, CreateCriticalErrorHandler(), messagePump, dispatcher, subscriptionManager, onMessage, localAddress);
            return startableEndpoint;
        }

        static IManageSubscriptions CreateSubscriptionManager(TransportInfrastructure transportInfra)
        {
            var subscriptionInfra = transportInfra.ConfigureSubscriptionInfrastructure();
            var factoryProperty = typeof(TransportSubscriptionInfrastructure).GetProperty("SubscriptionManagerFactory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var factoryInstance = (Func<IManageSubscriptions>)factoryProperty.GetValue(subscriptionInfra, new object[0]);
            return factoryInstance();
        }

        static void RegisterReceivingComponent(SettingsHolder settings, string localAddress)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;
            var parameters = new[]
            {
                typeof(LogicalAddress),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(TransportTransactionMode),
                typeof(PushRuntimeSettings),
                typeof(bool)
            };
            var ctor = typeof(Endpoint).Assembly.GetType("NServiceBus.ReceiveConfiguration", true).GetConstructor(flags, null, parameters, null);

            var receiveConfig = ctor.Invoke(new object[] { null, localAddress, localAddress, null, null, null, false });
            settings.Set("NServiceBus.ReceiveConfiguration", receiveConfig);
        }

        string GetInstallationUserName()
        {
            return settings.TryGet("NServiceBus.Raw.Identity", out string username)
                ? username
                : DefaultName();
        }

        static string DefaultName()
        {
#if NET452
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
#else
            return null;
#endif
        }

        string GetConnectionString(TransportDefinition transportDefinition)
        {
            var instance = settings.Get(connectionStringType.FullName);
            return (string) connectionStringGetter.Invoke(instance, new object[] {transportDefinition});
        }

        RawCriticalError CreateCriticalErrorHandler()
        {
            settings.TryGet("onCriticalErrorAction", out Func<ICriticalErrorContext, Task> errorAction);
            return new RawCriticalError(errorAction);
        }

        SettingsHolder settings;
        Func<MessageContext, IDispatchMessages, Task> onMessage;

        static Type connectionStringType = typeof(IEndpointInstance).Assembly.GetType("NServiceBus.TransportConnectionString", true);
        static MethodInfo connectionStringGetter = connectionStringType.GetMethod("GetConnectionStringOrRaiseError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
}