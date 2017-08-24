using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

static class RawEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRawEndpoint<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        string name, Func<MessageContext, TContext, IDispatchMessages, Task> onMessage,
        Func<IReceivingRawEndpoint, TContext, Task> onStarted = null, 
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
    {
        var component = new RawEndpointComponent<TContext>(name, onMessage, onStarted, configure);
        return scenario.WithComponent(component);
    }

    public static Task Send(this IRawEndpoint endpoint, string destination, Dictionary<string, string> headers, byte[] body)
    {
        var op = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, body), new UnicastAddressTag(destination));
        return endpoint.Dispatch(new TransportOperations(op), new TransportTransaction(), new ContextBag());
    }
}

class RawEndpointComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    string name;
    Func<MessageContext, TContext, IDispatchMessages, Task> onMessage;
    Func<IReceivingRawEndpoint, TContext, Task> onStarted;
    Action<RawEndpointConfiguration> configure;

    public RawEndpointComponent(string name, Func<MessageContext, TContext, IDispatchMessages, Task> onMessage, Func<IReceivingRawEndpoint, TContext, Task> onStarted, Action<RawEndpointConfiguration> configure)
    {
        this.name = name;
        this.onMessage = onMessage;
        this.onStarted = onStarted;
        this.configure = configure;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var typedScenarioContext = (TContext) run.ScenarioContext;

        var config = RawEndpointConfiguration.Create(name, (c, d) => onMessage(c, typedScenarioContext, d), "poison");
        config.AutoCreateQueue();
        configure?.Invoke(config);

        if (!config.Settings.TryGet(out TransportDefinition _))
        {
            config.UseTransport<LearningTransport>();
        }
        return Task.FromResult<ComponentRunner>(new Runner(config, name, endpoint => onStarted != null ? onStarted(endpoint, typedScenarioContext) : Task.FromResult(0)));
    }

    class Runner : ComponentRunner
    {
        RawEndpointConfiguration config;
        IReceivingRawEndpoint endpoint;
        Func<IReceivingRawEndpoint, Task> onStarted;

        public Runner(RawEndpointConfiguration config, string name, Func<IReceivingRawEndpoint, Task> onStarted)
        {
            this.config = config;
            this.Name = name;
            this.onStarted = onStarted;
        }

        public override async Task ComponentsStarted(CancellationToken token)
        {
            await onStarted(endpoint).ConfigureAwait(false);
        }

        public override async Task Start(CancellationToken token)
        {
            endpoint = await RawEndpoint.Start(config).ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            if (endpoint != null)
            {
                await endpoint.Stop().ConfigureAwait(false);
            }
        }

        public override string Name { get; }
    }
}