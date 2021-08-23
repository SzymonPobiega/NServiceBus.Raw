using System;
using System.Reflection;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System.Threading;

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(SettingsHolder settings, Func<MessageContext, IMessageDispatcher, Task> onMessage)
        {
            this.settings = settings;
            this.onMessage = onMessage;
        }

        public async Task<IStartableRawEndpoint> Initialize()
        {
            CreateCriticalErrorHandler();

            CreateStartupDiagnostics();

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = settings.GetConnectionString();
            var transportInfrastructure = await transportDefinition.Initialize(settings, connectionString).ConfigureAwait(false);
            settings.Set(transportInfrastructure);

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(settings.EndpointName()));
            var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? settings.EndpointName();
            var mainQueueAddress = new QueueAddress(baseQueueName, null, mainInstance.Properties, null);
            var localAddress = mainQueueAddress; //TODO: Is this correct?
            settings.SetDefault(mainQueueAddress);

            IMessageReceiver messagePump = null;
            ISubscriptionManager subscriptionManager = null;

            if (!settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                RegisterReceivingComponent(settings, mainQueueAddress, localAddress);

                var receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
                var queueCreator = receiveInfrastructure.QueueCreatorFactory();
                messagePump = receiveInfrastructure.MessagePumpFactory();
                var queueBindings = settings.Get<QueueBindings>();
                queueBindings.BindReceiving(localAddress);

                if (settings.GetOrDefault<bool>("NServiceBus.Raw.CreateQueue"))
                {
                    await queueCreator.CreateQueueIfNecessary(queueBindings, GetInstallationUserName()).ConfigureAwait(false);
                }

                if (transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast ||
                    transportInfrastructure.OutboundRoutingPolicy.Sends == OutboundRoutingType.Multicast)
                {
                    subscriptionManager = CreateSubscriptionManager(transportInfrastructure);
                }
            }

            var sendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
            var dispatcher = sendInfrastructure.DispatcherFactory();

            await transportInfrastructure.Start().ConfigureAwait(false);

            var startableEndpoint = new StartableRawEndpoint(settings, transportInfrastructure, CreateCriticalErrorHandler(), messagePump, dispatcher, subscriptionManager, onMessage, localAddress);
            return startableEndpoint;
        }

        void CreateStartupDiagnostics()
        {
            var ctor = hostingSettingsType.GetConstructors()[0];
            var hostingSettings = ctor.Invoke(new object[] { settings });
            settings.Set(hostingSettingsType.FullName, hostingSettings);
        }

        static ISubscriptionManager CreateSubscriptionManager(TransportInfrastructure transportInfra)
        {
            var subscriptionInfra = transportInfra.ConfigureSubscriptionInfrastructure();
            var factoryProperty = typeof(TransportSubscriptionInfrastructure).GetProperty("SubscriptionManagerFactory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var factoryInstance = (Func<IManageSubscriptions>)factoryProperty.GetValue(subscriptionInfra, new object[0]);
            return factoryInstance();
        }

        static void RegisterReceivingComponent(SettingsHolder settings, QueueAddress logicalAddress, string localAddress)
        {
            var type = typeof(Endpoint).Assembly.GetType("NServiceBus.ReceiveComponent+Configuration", true);
            var ctor = type.GetConstructors()[0];

            var receiveConfig = ctor.Invoke(new object[] {
                logicalAddress, //logicalAddress
                localAddress, //queueNameBase
                localAddress, //localAddress
                null, //instanceSpecificQueue
                null, //transactionMode
                null, //pushRuntimeSettings
                false, //purgeOnStartup
                null, //pipelineCompletedSubscribers
                false, //isSendOnlyEndpoint
                null, //executeTheseHandlersFirst
                null, //messageHandlerRegistry
                false, //createQueues
                null, //transportSeam
            });

            settings.Set("NServiceBus.ReceiveComponent+Configuration", receiveConfig);
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

        RawCriticalError CreateCriticalErrorHandler()
        {
            settings.TryGet("onCriticalErrorAction", out Func<ICriticalErrorContext, CancellationToken, Task> errorAction);
            return new RawCriticalError(errorAction);
        }

        SettingsHolder settings;
        Func<MessageContext, IMessageDispatcher, Task> onMessage;

        static Type hostingSettingsType = typeof(IEndpointInstance).Assembly.GetType("NServiceBus.HostingComponent+Settings", true);
    }
}